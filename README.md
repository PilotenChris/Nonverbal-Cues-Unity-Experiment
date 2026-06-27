# Nonverbal Cues Unity Experiment

## Project Overview

This project is an MVP (Minimum Viable Product) developed as an experimental environment for the [VR Full-Body Tracking for Extracting Nonverbal Cues](https://github.com/PilotenChris/VR-Full-Body-Tracking-for-Extracting-Nonverbal-Cues) project.

This project receives the extracted nonverbal cues over UDP. It combines these cues with the user's spoken conversation before sending the complete context to the  LLM-powered NPC. This allows the NPC to better understand the user's intent and behavior during interactions.

## Unity-specific Features

All cues from the Python RNN project are supported. This shows the specific nonverbal cues implemented in the Unity project:

1. **Conversation Stage**
    - Listening (When NPC is talking)
    - Thinking (When no one is talking)
    - Speaking (When user is talking)
2. **User viewing position**
    - Looking at NPC
    - Glancing at NPC
    - Looking near NPC
    - Looking away from NPC
3. **Player distance**
    - User within 2.5 m of NPC (NPC will look at user and user can talk to it)
    - User is more than 2.5 m away from the NPC (the NPC no longer looks at the user and interaction is disabled)

## Technologies Used
- **Speech to Text (STT)**: OpenAI Whisper via the huggingface API.
- **Large Language Model (LLM)**: Llama 3.1 8B Instant via the GroqCloud API.
- **Text to Speech (TTS)**: Speechify API.

## Usage

1. Select if nonverbal cues is going to be used or not.
2. Start the Unity project.
    - Start Python Nonverbal cues extractor if nonverbal cues are going to be used.
3. User moves within 2.5 m of NPC.
4. Aim the controller at the NPC and hold the trigger while speaking. The controller may be moved freely while the trigger is held.
    - User can use their body movement at all point while the Unity project is running and the user is within 2.5m of the NPC.
5. Release the trigger to allow the NPC to process the conversation and respond.

## Future Work

- Integrate Live Caption to inject nonverbal cues into the conversation stream in real time instead of appending them afterward.
- Develop a domain-specific language model tailored for VR conversations and NPC interactions.
    1. Evaluate whether specialized language models outperform general-purpose LLMs for immersive VR interactions.