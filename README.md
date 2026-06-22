My expansion to the DancerGuidance system Talox built, with a focus on automating some of the club workflows that were done manually previously. Enjoy!

**Dance Event Suite Expansion:**

<img width="3820" height="3080" alt="Dance Event Suite 1 0" src="https://github.com/user-attachments/assets/db6701a8-4cc2-4746-b47a-a7e67518bafa" />


**New features:**

- **Anonymous "Request a dancer" feature**: only the audience member and the requested dancer can see the request.
- **Event manager mode**: Click OHD button to assign "Birthday!" or "Freshie!" state to audience members.
- **Staff mode**: Hides your display, but shows audience displays
- **Media mode**: Hides all displays, including your own.
- **Displays are now truly hidden when hidden**. (Can't be clicked on or raycasted to anymore)
- **Checkmarks** that indicate dance fulfillment to each dancer.
- Entire system hidden from the VRC camera to not ruin event pictures.

- **"No dances" state.** (Can be self-assigned)
- **Compact design**: One button for audience interactions, counter within the button, other OHD elements only visible when necessary.

- OverHeadNumber options for dance count customization.
  - 0 = White
  - 1 - Dances Needed = Green
  - Dances Needed - Dances No Longer Needed = Orange
  - Dances No Longer Needed - Max Dances = Red
  - Max Dances = No Dances Text (Red)
  - Incrementing the counter after reaching "No Dances" state resets the counter.
  - Made counters invisible for cameras.

<img width="526" height="469" alt="OHD" src="https://github.com/user-attachments/assets/aa920b47-84f1-4655-ac7e-cc91523d7333" />

- Offset (Counters position from the player it belongs to): XYZ values (Y decreased to 0.6 in this image)
- Click Delay (Seconds in between each dance counter increment): "Number" seconds. (Decreased to 0.1 seconds in this image)
- Keep Alive (How long dance count is persisted): "Number" hours. (Decreased to 3 hours in this image)

<video src="https://github.com/user-attachments/assets/6c091397-97ab-4eee-b08b-82812854dcc0" autoplay loop muted playsinline width="100%"></video>

<video src="https://github.com/user-attachments/assets/00cfe19a-f8e1-4115-86ce-721494abbe0c" autoplay loop muted playsinline width="100%"></video>

<video src="https://github.com/user-attachments/assets/db7838da-ec08-44fd-a597-34edd913ac28" autoplay loop muted playsinline width="100%"></video>

# How to use

1. Grab the latest unitypackage from: https://github.com/CodeyFlex/DancerGuidanceCompact/releases
2. Add the package to your world project
3. Add both the enable button prefab to the scene and place it on a location where your dancers can access it
4. Add the Overhead number to the scene and change any parameters on the udon script to your liking

-----------------* Original Text from Talox *-----------------

This repo was made at the request of multiple people, and with the release of Persistence, this was made a lot easier to put together. The people at El Diablo (ZAPZARAP and Iferia) helped a great deal with the integration side of things. 

# How to request a feature

You can poke me on discord here: [discord](https://discord.gg/bJKDe6eEVx)
