#!/bin/bash

# Exit immediately if a command exits with a non-zero status
set -e

# Variables
IMAGE_NAME="delasrc/cockroach2postgres"
TAG=$(date +%Y%m%d)


# Step 1: Ensure multi-platform builder is active
echo "Setting up Buildx multi-platform builder..."
docker buildx use multiarch-builder

# Step 2: Build and push the Docker image for multiple platforms
echo "Building Docker image for amd64 and arm64..."
docker buildx build --platform linux/amd64,linux/arm64 \
    -t $IMAGE_NAME:$TAG \
    -t $IMAGE_NAME:latest \
    --push .

# Step 3: Push the Docker image to Docker Hub
echo "Pushing Docker image to Docker Hub..."
docker push $IMAGE_NAME:$TAG
docker push $IMAGE_NAME:latest

# Success message
echo "Docker image $IMAGE_NAME:$TAG has been successfully built and pushed."
