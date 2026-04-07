# VRChat OSC access notes

This document records the OSC access rules used by the Blockly integration.

## Read-only avatar parameters

VRChat built-in avatar parameters are read-only. OSCControl should allow rules to
listen to them, but should not generate `vrchat.param <name> = ...` writes for
these names:

```text
IsLocal
PreviewMode
Viseme
Voice
GestureLeft
GestureRight
GestureLeftWeight
GestureRightWeight
AngularY
VelocityX
VelocityY
VelocityZ
VelocityMagnitude
Upright
Grounded
Seated
AFK
TrackingType
VRMode
MuteSelf
InStation
Earmuffs
IsOnFriendsList
AvatarVersion
IsAnimatorEnabled
ScaleModified
ScaleFactor
ScaleFactorInverse
EyeHeightAsMeters
EyeHeightAsPercent
```

Blockly exposes these through the `when VRChat read-only built-in param ...`
trigger block.

## Writable avatar parameters

Custom avatar parameters can be controlled by VRChat features such as Expression
Menus, Avatar Parameter Driver, and OSC when the avatar is configured for it.
Blockly write blocks should default to a custom name such as `CustomParam`, not a
VRChat built-in name.

## Other writable VRChat OSC endpoints

The following OSC areas are treated as commands sent to VRChat:

```text
/input/<Name>
/chatbox/input
/chatbox/typing
```

Tracking and eye-tracking OSC endpoints are also inputs into VRChat, but they are
not currently modeled as first-class Blockly blocks in OSCControl.

## Sources

- https://creators.vrchat.com/avatars/animator-parameters/
- https://docs.vrchat.com/docs/osc-avatar-parameters
- https://docs.vrchat.com/docs/osc-as-input-controller
- https://docs.vrchat.com/docs/osc-chatbox
- https://docs.vrchat.com/docs/osc-trackers
- https://docs.vrchat.com/docs/osc-eye-tracking
