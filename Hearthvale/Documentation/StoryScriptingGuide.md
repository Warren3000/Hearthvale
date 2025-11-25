# Hearthvale - Story Scripting Guide

*Created: 2025-09-09*

## Table of Contents

1. [Introduction](#introduction)
2. [Story Structure Approaches](#story-structure-approaches)
3. [MonoGame Implementation Methods](#monogame-implementation-methods)
4. [Code Examples](#code-examples)
5. [Story Content Organization](#story-content-organization)
6. [Fantasy Action RPG Specific Elements](#fantasy-action-rpg-specific-elements)
7. [Development Roadmap](#development-roadmap)

## Introduction

This guide outlines approaches to scripting a compelling story for Hearthvale, an action RPG set in a fantasy world, built with a custom MonoGame engine. The document serves as a reference point for implementing narrative systems and content creation strategies.

## Story Structure Approaches

### 1. Main Quest + Side Quests Structure
- Create a central narrative that drives the main storyline
- Develop optional side quests that flesh out the world and characters
- Allow player choices to influence quest outcomes

### 2. Multi-Act Structure
- Introduction (tutorial + world establishment)
- Rising action (character development, expanding world)
- Climax (major confrontation)
- Resolution (aftermath and consequences)

### 3. Hub-Based Storytelling
- Central town/village that serves as a story hub
- Story branches out as player completes quests and unlocks new areas
- NPCs in the hub evolve and change as the story progresses

### 4. Data-Driven Story Management
- Store story content in JSON or XML files that can be easily edited
- Create parsers to load and process this content at runtime
- Separate story content from game logic for easier editing

### 5. State Machine Story Progression
- Implement a state machine to track story progress
- Define clear transitions between story states based on player actions
- Great for maintaining a coherent narrative flow

### 6. Event-Based Storytelling
- Create an event system where game events trigger story progression
- Allow for non-linear storytelling as events can occur in different orders
- Good for reactive storytelling that responds to player choices

## MonoGame Implementation Methods

### Dialogue System
- Text rendering and management
- Speaker identification
- Dialogue choices and branching
- Conversation state tracking

### Quest System
- Quest states (available, active, completed, failed)
- Objective tracking
- Quest dependencies and prerequisites
- Reward distribution

### Story Flags/Variables
- Boolean flags for story progress
- Numerical variables for counting/tracking
- Persistence between game sessions
- Condition checking for story progression

### Scene Management
- Cutscene sequencing
- Camera control for storytelling
- Character positioning and animation
- Timing and pacing control

## Code Examples

### Dialogue System

```csharp
public class DialogueSystem
{
    private List<DialogueLine> currentDialogue;
    private int currentLineIndex;
    private SpriteFont dialogueFont;
    private Texture2D dialogueBox;
    
    public DialogueSystem(GraphicsDevice graphicsDevice, ContentManager content)
    {
        dialogueFont = content.Load<SpriteFont>("Fonts/DialogueFont");
        dialogueBox = content.Load<Texture2D>("UI/DialogueBox");
    }
    
    public void StartDialogue(List<DialogueLine> dialogue)
    {
        currentDialogue = dialogue;
        currentLineIndex = 0;
    }
    
    public void Update(GameTime gameTime)
    {
        // Check for input to advance dialogue
        if (InputManager.WasKeyPressed(Keys.Space) && currentDialogue != null)
        {
            currentLineIndex++;
            if (currentLineIndex >= currentDialogue.Count)
            {
                EndDialogue();
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        if (currentDialogue != null && currentLineIndex < currentDialogue.Count)
        {
            // Draw dialogue box
            spriteBatch.Draw(dialogueBox, new Rectangle(50, 350, 700, 150), Color.White);
            
            // Draw text
            DialogueLine line = currentDialogue[currentLineIndex];
            spriteBatch.DrawString(dialogueFont, line.SpeakerName + ":", new Vector2(70, 370), Color.Gold);
            spriteBatch.DrawString(dialogueFont, line.Text, new Vector2(70, 400), Color.White);
        }
    }
    
    private void EndDialogue()
    {
        currentDialogue = null;
        // Trigger any post-dialogue events
    }
}

public class DialogueLine
{
    public string SpeakerName { get; set; }
    public string Text { get; set; }
    public List<DialogueChoice> Choices { get; set; }
    
    public DialogueLine(string speaker, string text)
    {
        SpeakerName = speaker;
        Text = text;
        Choices = new List<DialogueChoice>();
    }
}
```

### Quest System

```csharp
public class QuestSystem
{
    private Dictionary<string, Quest> allQuests;
    private List<string> activeQuestIds;
    private Dictionary<string, bool> storyFlags;
    
    public QuestSystem()
    {
        allQuests = new Dictionary<string, Quest>();
        activeQuestIds = new List<string>();
        storyFlags = new Dictionary<string, bool>();
    }
    
    public void LoadQuestsFromFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        List<Quest> questList = JsonConvert.DeserializeObject<List<Quest>>(json);
        
        foreach (var quest in questList)
        {
            allQuests.Add(quest.Id, quest);
        }
    }
    
    public void ActivateQuest(string questId)
    {
        if (allQuests.ContainsKey(questId) && !activeQuestIds.Contains(questId))
        {
            activeQuestIds.Add(questId);
            allQuests[questId].State = QuestState.Active;
            // Fire quest activated event
        }
    }
    
    public void CompleteObjective(string questId, string objectiveId)
    {
        if (allQuests.ContainsKey(questId) && activeQuestIds.Contains(questId))
        {
            allQuests[questId].CompleteObjective(objectiveId);
            
            // Check if all objectives are complete
            if (allQuests[questId].AllObjectivesComplete())
            {
                CompleteQuest(questId);
            }
        }
    }
    
    public void CompleteQuest(string questId)
    {
        if (allQuests.ContainsKey(questId) && activeQuestIds.Contains(questId))
        {
            allQuests[questId].State = QuestState.Completed;
            activeQuestIds.Remove(questId);
            
            // Set story flags related to quest completion
            SetStoryFlag("completed_" + questId, true);
            
            // Check for follow-up quests
            foreach (var quest in allQuests.Values)
            {
                if (quest.PrerequisiteQuestIds.Contains(questId) && !activeQuestIds.Contains(quest.Id))
                {
                    ActivateQuest(quest.Id);
                }
            }
        }
    }
    
    public void SetStoryFlag(string flagName, bool value)
    {
        storyFlags[flagName] = value;
        // Check for any quests that might be affected by this flag
        CheckFlagDependentQuests();
    }
    
    public bool GetStoryFlag(string flagName)
    {
        return storyFlags.ContainsKey(flagName) && storyFlags[flagName];
    }
    
    private void CheckFlagDependentQuests()
    {
        // Logic to check if any quests should be activated based on story flags
    }
}
```

### JSON Quest/Story Data Format

```json
[
  {
    "id": "main_quest_1",
    "title": "The Call to Adventure",
    "description": "Investigate the strange happenings in Hearthvale Forest.",
    "questType": "Main",
    "prerequisiteQuestIds": [],
    "requiredFlags": ["intro_complete"],
    "objectives": [
      {
        "id": "speak_to_elder",
        "description": "Speak to Elder Thorne about the forest",
        "completed": false
      },
      {
        "id": "find_artifact",
        "description": "Find the ancient artifact in the forest ruins",
        "completed": false
      }
    ],
    "rewards": [
      {
        "type": "Item",
        "itemId": "elder_amulet",
        "quantity": 1
      },
      {
        "type": "Experience",
        "amount": 500
      }
    ],
    "dialogues": [
      {
        "id": "quest_start",
        "lines": [
          {
            "speaker": "Elder Thorne",
            "text": "The forest grows darker with each passing day. Something ancient has awakened."
          },
          {
            "speaker": "Player",
            "text": "What do you think is causing it?",
            "choices": [
              {
                "text": "I'll help investigate.",
                "nextDialogueId": "accept_quest"
              },
              {
                "text": "That sounds dangerous.",
                "nextDialogueId": "reluctant_accept"
              }
            ]
          }
        ]
      },
      {
        "id": "accept_quest",
        "lines": [
          {
            "speaker": "Elder Thorne",
            "text": "You must find the ancient artifact in the forest ruins. It may hold the key to understanding these events."
          }
        ]
      }
    ]
  }
]
```

## Story Content Organization

### 1. Create a Story Bible
- Document your world's history, factions, key characters, and locations
- Outline major story arcs and how they connect
- Define the theme and tone of your story

### 2. Story Flowcharting
- Use flowcharts to visualize story progression and branching paths
- Map out how player choices affect the narrative
- Identify key decision points and their consequences

### 3. Content Management
- Separate story content into modular files (by region, quest line, or character)
- Create a naming convention for story assets and elements
- Use version control (Git) to track changes to story content

### 4. File Structure Example
```
/Content
  /Story
    /MainQuests
      main_quest_1.json
      main_quest_2.json
    /SideQuests
      side_quest_region1.json
      side_quest_region2.json
    /Characters
      player.json
      companions.json
      villains.json
    /Dialogues
      region1_dialogues.json
      region2_dialogues.json
    /Cutscenes
      intro_cutscene.json
      finale_cutscene.json
  /StoryAssets
    /Portraits
    /BackgroundImages
    /CutsceneElements
```

## Fantasy Action RPG Specific Elements

### 1. Character Growth System
- Tie character progression to story milestones
- Allow skills/abilities to unlock through story progression
- Create personal arcs for companion characters

### 2. World-Building Through Discovery
- Hide lore in collectible items, books, or environmental storytelling
- Reveal the world's history gradually as players explore
- Use visual storytelling (ruins, monuments, etc.) to complement text

### 3. Faction System
- Create multiple factions with conflicting goals
- Allow player choices to affect standing with these factions
- Consequences for faction relationships impact available quests and story paths

### 4. Combat Narrative Integration
- Boss battles that advance the story
- Special abilities that unlock during pivotal story moments
- Environmental storytelling during combat sequences

## Development Roadmap

1. **Phase 1: Core Systems**
   - Implement basic dialogue system
   - Create quest tracking framework
   - Establish story flag system

2. **Phase 2: Content Creation**
   - Develop story bible and world lore
   - Write main quest storyline
   - Create key characters and dialogues

3. **Phase 3: Integration**
   - Connect story elements to gameplay systems
   - Implement story triggers and events
   - Create cutscenes for key story moments

4. **Phase 4: Refinement**
   - Playtesting and narrative flow adjustments
   - Polish dialogue and character interactions
   - Ensure story and gameplay balance

---

*Document maintained by: Warren3000*  
*Last updated: 2025-09-09*
