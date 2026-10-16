import json
from types import SimpleNamespace
from unittest.mock import AsyncMock, Mock
import pytest
from pydantic import BaseModel
from bthr_agent.models import Models, ModelFailure


class Answer(BaseModel):
    message: str


def settings(tmp_path, provider="ollama", local_only=False):
    deployment = "local" if provider in ("ollama", "compatible") else "cloud"
    profile = {"provider": provider, "model": "fixture", "deployment": deployment, "vision": True}
    if deployment == "local":
        profile["base_url"] = "http://127.0.0.1:11434"
    config = tmp_path / "models.yml"
    config.write_text(json.dumps({"tasks": dict.fromkeys(["conversation", "insights", "vision"], "test"), "profiles": {"test": profile}}))
    pricing = tmp_path / "pricing.yml"
    pricing.write_text('{"rates": {}}')
    return SimpleNamespace(model_config_path=config, pricing_config_path=pricing, local_only=local_only)


@pytest.mark.parametrize("provider", ["anthropic", "openai", "ollama", "compatible"])
async def test_shared_output_and_one_accounting_event_per_attempt(tmp_path, provider):
    accounting = Mock()
    models = Models(settings(tmp_path, provider), accounting)
    call = AsyncMock(return_value=SimpleNamespace(content='{"message":"ok"}', usage_metadata={"input_tokens": 10, "output_tokens": 2}, response_metadata={"model": "fixture"}))
    models.client = lambda _: SimpleNamespace(ainvoke=call)
    result = await models.json("conversation", "instruction", "request", Answer)
    assert result.message == "ok"
    call.assert_awaited_once()
    accounting.record.assert_called_once()
    assert accounting.record.call_args.args[-1] == "success"


async def test_bad_model_output_is_counted_and_never_leaks(tmp_path):
    accounting = Mock()
    models = Models(settings(tmp_path), accounting)
    models.client = lambda _: SimpleNamespace(ainvoke=AsyncMock(return_value=SimpleNamespace(
        content="SECRET-CANARY invalid json", usage_metadata={"input_tokens": 10, "output_tokens": 5}, response_metadata={})))
    with pytest.raises(ModelFailure) as error:
        await models.json("conversation", "prompt", "user", Answer)
    assert "SECRET-CANARY" not in str(error.value)
    accounting.record.assert_called_once()
    assert accounting.record.call_args.args[2]["output_tokens"] == 5


@pytest.mark.parametrize("provider", ["anthropic", "openai"])
def test_local_only_rejects_cloud_before_constructing_clients(tmp_path, provider):
    with pytest.raises(ValueError, match="LOCAL_ONLY"):
        Models(settings(tmp_path, provider, local_only=True), Mock())
