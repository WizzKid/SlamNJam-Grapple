# SlamNJam-Grapple

Hello, this code will not function properly without our AI scripts and correctly tagged enemies in Unity.

High level explanation of the Grapple:
  - Cooldown based skillshot.
  - Physics/addforce based on the way out.
  - On collision with anything, if it reaches max range, or if it times out, it comes back with a lerp.
  - Option of bringing the enemy back stunned or bringing you to a large enemy or object if the tag matches.
  - Destroys the hook on return by collision with player, distance, or timeout

In this repository you will find:

Grapple Input CD - Input and Cool Down for Grapple Hook.
Grapple Logic - Grapple Hook behavior after input is detected.
