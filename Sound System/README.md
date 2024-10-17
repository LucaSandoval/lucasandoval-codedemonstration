# Sound System snipped taken from "Rusalka"

Link to [Itch.io Page](https://ragefordragons.itch.io/rusalka).

The following is the base code that handles the Sound System in all of my projects- this particular version is the latest, adapted for Rusalka. It's extremely versatile, with support for nearly any common sound need in a game- such as fading sounds in and out,
layered sounds (for dynamic music etc..), playing sounds at random pitches and so forth. The system was designed to be as 'plug and play' as possible- requiring essentially only the import of these two scripts to function. The sound player is a singleton that provides public methods for controlling sounds from wherever they are needed in the Unity project. Sounds are stored as scriptable objects with tweakable parameters for rapid iteration on sound effect volume, looping, and pitch settings. 
