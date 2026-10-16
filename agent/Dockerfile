FROM python:3.12.13-slim
WORKDIR /app
COPY pyproject.toml ./
COPY src ./src
COPY config ./config
RUN pip install --no-cache-dir . && useradd --uid 10001 --create-home agent
USER 10001
EXPOSE 8090
HEALTHCHECK --interval=30s --timeout=5s CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8090/health/live',timeout=3)"
CMD ["uvicorn", "bthr_agent.app:app", "--host", "0.0.0.0", "--port", "8090", "--workers", "1", "--no-access-log"]
