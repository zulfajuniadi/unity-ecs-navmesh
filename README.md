# Unity ECS with navmesh and MapBox

A demo implementation of Unity Entity Component System with NavMesh and [MapBox Unity](https://www.mapbox.com/unity/) SDK.

I'm getting around 40-50 FPS with 100,000 entities traversing the navmesh.

[!["100000 Navmesh Agents, MapBox SDK](https://i.vimeocdn.com/video/705015074_300x170.webp)](https://vimeo.com/273263679 "100000 Navmesh Agents, MapBox SDK")

Requires Unity version 2018.1 or newer to run. Scripting Runtime Version has to be set to .NET 4.0 and the ECS packages installed via the package manager.

## Path Finding Usage

The navmesh queries are jobified which means that it will try run on all cores. The main script is the `NavMeshQuerySystem.cs`. To use it in your project, just include that file. There are 3 methods to call when using the NavMeshQuery class:

`void RegisterPathResolvedCallback(SuccessQueryDelegate callback)` is used to register the path success handler.

`void RegisterPathFailedCallback(FailedQueryDelegate callback)` is used to register the path failed handler.

`void RequestPath (int id, Vector3 from, Vector3 to)` is used to query the navmesh. The `id` field is for you to set. In this example project the ID is set to entity ID to determine which result belongs to which entity.

Upon successful query, all registered success callbacks will be called with the `id` of the request and the `Vector3[]` path. On failure, all registered failure callbacks will be called with the `id` and the `PathfindingFailedReason` enum.

Static counterparts of the methods can also be called. This enables monobehaviours and other classes to query the navmesh without the needing injection via the ECS system:

`static void RegisterPathResolvedCallbackStatic (SuccessQueryDelegate callback)`

`static void RegisterPathFailedCallbackStatic (FailedQueryDelegate callback)`

`static void RequestPathStatic (int id, Vector3 from, Vector3 to)`

## NavAgent Usage

If you want to use the built in NavAgent Component, you can just add that to your existing archetype. This will allow you to set the entity's destination via the `void SetDestination (Entity entity, NavAgent agent, Vector3 destination)` method. Calling this will request a path from the NavMesh and move the agent once the path has been resolved. Alternatively the `static void SetDestinationStatic (Entity entity, NavAgent agent, Vector3 destination)` is also available.

There are a few properties to set from an agent:

1. Stopping Distance
2. Accelleration
3. Max Move Speed
4. Rotation Speed

You can also have your own Position and Rotation component on the agent. To sync the position and rotation component, be sure to add a combination of these components according to your needs:

1. `SyncPositionToNavAgent`: This will copy the Position Component's Value to the NavAgent prior movement.
2. `SyncPositionFromNavAgent`: This will copy the rotation of the NavAgent to the Position Component after movement.
3. `SyncRotationToNavAgent`: This will copy the Rotation Component's Value to the NavAgent prior movement.
4. `SyncRotationFromNavAgent`: This will copy the rotation of the NavAgent to the Rotation Component after movement.

An example NavAgent archetype would have these components:

```cs
var agent = Getmanager ().CreateArchetype (
    typeof (NavAgent),
    typeof (Position),                 // optional
    typeof (Rotation),                 // optional
    typeof (SyncPositionToNavAgent),   // optional
    typeof (SyncRotationToNavAgent),   // optional
    typeof (SyncPositionFromNavAgent), // optional
    typeof (SyncRotationFromNavAgent), // optional
    typeof (TransformMatrix)           // optional for instanced mesh rendering
);
```

## License

Copyright 2018 Zulfa Juniadi

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.