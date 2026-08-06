FROM ubuntu:22.04

# No prompts plz
ENV DEBIAN_FRONTEND=noninteractive

# Build dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    build-essential \
    cmake \
    python3 \
    git \
    mingw-w64

