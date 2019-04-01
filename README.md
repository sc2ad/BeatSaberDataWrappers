# BeatSaberDataWrappers
Wraps most of the useful information into a static class that can be accessed on runtime without reflection.
## How to use
Add this project as a reference, ensure that it exists as a Plugin, and access `Data.x` in order to access various properties on runtime.
You may also hook onto `Data.statusChange` to receive status updates.
TODO: Add safety for when this mod does not exist as a Plugin (this may use reflection).
