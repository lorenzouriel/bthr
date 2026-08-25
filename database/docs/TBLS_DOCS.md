## How to Generate `tbls` Database Documentation

### Run `docker pull` to pull the image
```bash
docker pull ghcr.io/k1low/tbls:latest
```

### Add the `tbls.yml`
```yml
# .tbls.yml
# DSN (Database Source Name) to connect database
dsn: postgres://user:password@host:5432/database?sslmode=disable

# Path to generate document
# Default is `dbdoc`
docPath: schema
```

### Generate the docs
```bash
docker run --rm -v ${PWD}:/work -w /work ghcr.io/k1low/tbls doc
```