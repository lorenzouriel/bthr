from pathlib import Path
from pydantic import Field, SecretStr
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore", hide_input_in_errors=True)
    telegram_bot_token: SecretStr
    telegram_webhook_secret: SecretStr = Field(min_length=32)
    bot_api_key: SecretStr = Field(min_length=32)
    api_url: str = "http://api:8080"
    web_url: str = "http://localhost:8080"
    model_config_path: Path = Path("config/models.yml")
    pricing_config_path: Path = Path("config/model-pricing.yml")
    local_only: bool = True
    timezone: str = "UTC"
    environment: str = "Development"
    agent_instance: str = "agent-local"
    service_version: str = "local"
    otel_exporter_otlp_endpoint: str = "http://otel-collector:4317"
