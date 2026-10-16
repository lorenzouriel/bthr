from types import SimpleNamespace
from unittest.mock import AsyncMock, Mock
import pytest
from bthr_agent.workflow import Workflow, Action
from bthr_agent.photos import MealExtraction, normalize
from bthr_agent.api import UnknownMutation, ApiFailure
from bthr_agent.models import local_endpoint

RESOURCES = ["budgets", "bills", "earnings", "expenses", "investments", "goals", "workouts",
             "personal-records", "weekly-routines", "meals", "water-intake", "body-metrics", "sleep-logs",
             "habits", "habit-logs", "substance-logs", "symptom-logs", "meditation-sessions", "journal-entries"]


def workflow(action):
    api = SimpleNamespace(identity=AsyncMock(return_value={"userId": 7, "binding": "one"}),
                          request=AsyncMock(return_value={"id": 5}),
                          records=AsyncMock(return_value={"rows": [{"record": {"id": 5}, "etag": '"10"'}], "hasMore": False}))
    models = SimpleNamespace(json=AsyncMock(return_value=action))
    value = Workflow(api, models, SimpleNamespace(timezone="UTC"), SimpleNamespace(operations=Mock()))
    value.catalog = {r: {"path": "/api/users/{userId}/" + r, "create": {"type": "object", "properties": {}, "additionalProperties": False},
                         "update": {"type": "object", "properties": {}, "additionalProperties": False}} for r in RESOURCES}
    return value, api


@pytest.mark.parametrize("resource", RESOURCES)
async def test_all_resources_use_server_owner(resource):
    value, api = workflow(Action(operation="create", resource=resource))
    assert "Saved" in await value.run(123, "create a record")
    assert api.request.call_args.args[2] == "/api/users/7/" + resource


async def test_delete_requires_actor_bound_one_use_confirmation():
    value, api = workflow(Action(operation="delete", resource="meals", record_id=5))
    output = await value.run(123, "delete meal 5")
    assert "Delete this record?" in output
    api.request.assert_not_called()
    command = value.sessions[123].pending[0]
    assert "does not match" in await value.run(456, command)
    api.request.assert_not_called()
    assert "Deleted" in await value.run(123, command)
    assert api.request.call_args.kwargs["etag"] == '"10"'
    await value.run(123, command)
    assert api.request.await_count == 1


async def test_binding_change_invalidates_pending_confirmation():
    value, api = workflow(Action(operation="delete", resource="meals", record_id=5))
    await value.run(123, "delete meal 5")
    command = value.sessions[123].pending[0]
    api.identity.return_value = {"userId": 7, "binding": "new"}
    assert "does not match" in await value.run(123, command)
    api.request.assert_not_called()


async def test_model_cannot_add_owner_or_status_fields():
    value, api = workflow(Action(operation="create", resource="meals", values={"userId": 99}))
    assert "valid fields" in await value.run(123, "create")
    api.request.assert_not_called()


async def test_photo_upload_retry_never_recreates_meal():
    meal = MealExtraction(is_meal=True, description="rice", calories=100, carbs_grams=20)
    value, api = workflow(meal)
    api.request.side_effect = [{"id": 5}, ApiFailure("upload failed"), {"photo": "saved"}]
    assert "photo upload is incomplete" in await value.run(123, "lunch", [b"normalized-image"])
    assert "Saved meal" in await value.run(123, "/retry_photo")
    assert [call.args[1] for call in api.request.call_args_list] == ["POST", "PUT", "PUT"]


async def test_ambiguous_photo_create_clears_auto_retry_state():
    value, api = workflow(MealExtraction(is_meal=True, calories=100))
    api.request.side_effect = UnknownMutation("unknown")
    with pytest.raises(UnknownMutation):
        await value.run(123, "lunch", [b"image"])
    assert not value.sessions[123].images
    assert not value.sessions[123].photo_result


def test_local_endpoint_policy():
    assert local_endpoint("http://host.docker.internal:11434")
    assert local_endpoint("http://127.0.0.1:11434")
    assert not local_endpoint("https://api.openai.com/v1")
    assert not local_endpoint("http://127.0.0.1.evil.example")
    assert not local_endpoint("https://ollama.com")


def test_malformed_image_is_rejected():
    with pytest.raises(Exception):
        normalize(b"this is not an image")
