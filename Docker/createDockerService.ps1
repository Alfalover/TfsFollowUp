docker service create `
	--name tfsfollowup `
	--mount source=tfsfollowup.data,target=/App/Data `
	--publish 80:4321 `
	--secret tfsfollowup tfsfollowup 