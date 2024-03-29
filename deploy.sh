#!/usr/bin/env bash

set -Eeuo pipefail

if [ "$1" == "prod" ]; then
    docker-compose -p tyr -f ./docker-compose.prod.yml up -d --build
elif [ "$1" == "services" ]; then
    docker-compose -p tyr -f ./docker-compose.prod.yml up -d --build
    docker-compose -p dev-tyr -f ./docker-compose.dev.yml up -d --build
elif [ "$1" == "all" ]; then
    docker-compose -p infra-tyr -f ./docker-compose.infra.yml up -d --build
    docker-compose -p tyr -f ./docker-compose.prod.yml up -d --build
    docker-compose -p dev-tyr -f ./docker-compose.dev.yml up -d --build
elif [ "$1" == "down-all" ]; then
    docker-compose -p dev-tyr -f ./docker-compose.dev.yml down
    docker-compose -p tyr -f ./docker-compose.prod.yml down
    docker-compose -p infra-tyr -f ./docker-compose.infra.yml down
elif [ "$1" == "down-services" ]; then
    docker-compose -p dev-tyr -f ./docker-compose.dev.yml down
    docker-compose -p tyr -f ./docker-compose.prod.yml down
elif [ "$1" == "down-dev" ]; then
    docker-compose -p dev-tyr -f ./docker-compose.dev.yml down
elif [ "$1" == "down-prod" ]; then
    docker-compose -p tyr -f ./docker-compose.prod.yml down
elif [ "$1" == "down-infra" ]; then
    docker-compose -p infra-tyr -f ./docker-compose.infra.yml down
else
    docker-compose -p $1-tyr -f ./docker-compose.$1.yml up -d --build
fi
