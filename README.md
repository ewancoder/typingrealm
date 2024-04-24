# TypingRealm

This is a analytics and training tool to gather statistics and improve your typing. In future, more gaming elements will be added to it until it's a full-fledged game. But the first iteration of this project is simple: gather statistics on typing for future learning and improvement.

## Deploying the project locally

Just run in the root folder

```
docker-compose -f docker-compose-production.yml up --build -d
```

As of now, this file runs the project in localhost configuration (not the production configuration).

You can access the web page at `https://localhost` and APIs at `https://api.localhost/...` (for example, Typing api at `https://api.localhost/typing`).

Read `Caddyfile` for detailed reverse proxy mappings for different APIs.

## Running / Debugging the project locally

If you want to run the project in Debug mode, open `backend/TypingRealm.sln` solution and run the following projects:

`TypingRealm.Typing.Api`

Then separately, run `http-server -p 4200` in `frontend/src` folder.

You can access the web page at `http://localhost:4200` and APIs at `http://localhost:5000`.

> Note that currentyly you can access only Typing API at 5000 port because we only have one API. When we will have multiple APIs, they'll be accessible at 5001, 5002 etc ports or at some different ports, it will be listed in the documentation here.
