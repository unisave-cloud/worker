#!/bin/bash

# simple function deployment script to test things out

# this is not working, not in any way I try... (even with user:password)
REGISTRY_AUTH=$(kubectl get secret/do-registry-key -o jsonpath={.data.\\.dockerconfigjson})

curl -v -X POST --user root:password http://openfaas.unisave.local/system/functions -d '{
	"service": "backend-ff76637",
	"image": "registry.digitalocean.com/unisave/unisave-sandbox:0.5.0-dev",
	"labels": {
	    "backend-id": "1XlmtDgUHxdLeNGS"
	},
	"annotations": {
	    "backend-hash": "ff766373cf25ad4d3125443a1c2d3fb1",
	    "created-at": "2021-03-12T17:39:09.951942Z"
	},
	"envVars": {
		"INITIALIZATION_RECIPE_URL": "http://web.unisave-dev.svc.cluster.local:8000/_api/sandbox-recipe/123/pEKzqMtILv0CirCCYytGDoZyLCgYvLwi/39ea2c46ca1cbd03a463a9cd586c39d41903f45a4b608ef406fdf57254ea6914",
		"content_type": "application/json"
	},
	"secrets": ["do-registry-key"],
	"limits": {
		"memory": "70M"
    },
    "registryAuth": "'$REGISTRY_AUTH'"
}'
