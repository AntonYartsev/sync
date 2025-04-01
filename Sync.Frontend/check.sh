#!/bin/bash
docker build -t frontend-check .
docker run --rm frontend-check ls -la /app 