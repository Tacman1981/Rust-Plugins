using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Xml;

namespace Oxide.Plugins
{
    [Info("Trivia Plugin", "Tacman", "1.0.0")]
    [Description("A trivia plugin for your Rust server.")]
    public class TriviaPlugin : RustPlugin
    {
        private Dictionary<string, int> playerScores = new Dictionary<string, int>();
        private List<TriviaQuestion> triviaQuestions = new List<TriviaQuestion>();
        private Dictionary<string, int> rewardItems = new Dictionary<string, int>();
        private TriviaQuestion currentTriviaQuestion;

        private class TriviaQuestion
        {
            public string Question { get; set; }
            public string Answer { get; set; }
            public int Reward { get; set; }
        }

        private void Init()
        {
            LoadConfig();
            LoadTriviaQuestions();
            AskTriviaQuestion();
            timer.Every(600, AskTriviaQuestion);
        }

        private void LoadConfig()
{
    rewardItems = Config.Get<Dictionary<string, int>>("RewardItems");

    // Add "SR" as a special case for ServerRewards
    rewardItems["SR"] = 0;

    SaveConfig();
}

        protected override void LoadDefaultConfig()
        {
            Config["RewardItems"] = new Dictionary<string, int>
            {
                { "scrap", 10 },
                { "ShortnameOfSecondItem", 5 }
            };
            SaveConfig();
        }

        private void LoadTriviaQuestions()
        {
            var questionsPath = Path.Combine(Interface.Oxide.DataDirectory, "trivia_questions.json");

            if (!File.Exists(questionsPath))
            {
                // Create the default questions file if it doesn't exist
                CreateDefaultTriviaQuestions(questionsPath);
            }

            try
            {
                var json = File.ReadAllText(questionsPath);
                triviaQuestions = JsonConvert.DeserializeObject<List<TriviaQuestion>>(json);
            }
            catch (Exception ex)
            {
                Puts($"Error loading trivia questions: {ex.Message}");
            }
        }

        private void CreateDefaultTriviaQuestions(string filePath)
        {
            var defaultQuestions = new List<TriviaQuestion>
            {
                new TriviaQuestion
                {
                    Question = "What is the capital of France?",
                    Answer = "Paris"
                },
                new TriviaQuestion
                {
                    Question = "How many continents are there on Earth?",
                    Answer = "7"
                },
                new TriviaQuestion
                {
                    Question = "Who wrote the play 'Romeo and Juliet'?",
                    Answer = "William Shakespeare"
                },
                new TriviaQuestion
{
    Question = "What is the largest planet in our solar system?",
    Answer = "Jupiter"
},
new TriviaQuestion
{
    Question = "Which gas do plants absorb from the atmosphere during photosynthesis?",
    Answer = "Carbon dioxide"
},
new TriviaQuestion
{
    Question = "Who painted the 'Mona Lisa'?",
    Answer = "Leonardo da Vinci"
},
new TriviaQuestion
{
    Question = "What is the chemical symbol for gold?",
    Answer = "Au"
},
new TriviaQuestion
{
    Question = "In which year did the Titanic sink?",
    Answer = "1912"
},
new TriviaQuestion
{
    Question = "What is the smallest prime number?",
    Answer = "2"
},
new TriviaQuestion
{
    Question = "What is the largest mammal in the world?",
    Answer = "Blue whale"
},
new TriviaQuestion
{
    Question = "Who is the author of 'To Kill a Mockingbird'?",
    Answer = "Harper Lee"
},
new TriviaQuestion
{
    Question = "What is the national flower of Japan?",
    Answer = "Cherry blossom"
},
new TriviaQuestion
{
    Question = "Which gas makes up the majority of Earth's atmosphere?",
    Answer = "Nitrogen"
},
new TriviaQuestion
{
    Question = "What is the largest organ in the human body?",
    Answer = "Skin"
},
new TriviaQuestion
{
    Question = "Who is the current President of the United States (as of 2021)?",
    Answer = "Joe Biden"
},
new TriviaQuestion
{
    Question = "Which planet is known as the 'Red Planet'?",
    Answer = "Mars"
},
new TriviaQuestion
{
    Question = "What is the longest river in the world?",
    Answer = "Nile River"
},
new TriviaQuestion
{
    Question = "Which gas do humans primarily exhale when they breathe?",
    Answer = "Carbon dioxide"
},
new TriviaQuestion
{
    Question = "Who is the author of 'Pride and Prejudice'?",
    Answer = "Jane Austen"
},
new TriviaQuestion
{
    Question = "What is the chemical symbol for water?",
    Answer = "H2O"
},
new TriviaQuestion
{
    Question = "In which year did the Berlin Wall fall?",
    Answer = "1989"
},
new TriviaQuestion
{
    Question = "What is the largest desert in the world?",
    Answer = "Antarctica"
},
new TriviaQuestion
{
    Question = "Who is the founder of Microsoft?",
    Answer = "Bill Gates"
}
            };

            var json = JsonConvert.SerializeObject(defaultQuestions, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
            Puts("Default trivia questions file created.");
        }

        private Timer triviaQuestionTimer; // Declare a timer variable

private void AskTriviaQuestion()
{
    if (triviaQuestions.Count == 0)
        return; // No more questions to ask

    // Select a random question
    var random = new System.Random();
    currentTriviaQuestion = triviaQuestions[random.Next(triviaQuestions.Count)];

    // Loop through all online players and send the question to each of them
    foreach (var player in BasePlayer.activePlayerList)
    {
        player.ChatMessage($"Question: {currentTriviaQuestion.Question}");
    }

    // Cancel any previously set timers for DisplayAnswer
    if (triviaQuestionTimer != null)
    {
        triviaQuestionTimer.Destroy();
    }

    // Schedule a new timer to call DisplayAnswer after 30 seconds
    triviaQuestionTimer = timer.Once(30, () =>
    {
        DisplayAnswer(); // Call DisplayAnswer after 30 seconds
    });
}

        private void DisplayAnswer()
{
    bool answeredCorrectly = false;

    // Check answers of all players
    foreach (var player in BasePlayer.activePlayerList)
    {
        if (playerScores.ContainsKey(player.UserIDString) && playerScores[player.UserIDString] == currentTriviaQuestion.Reward)
        {
            answeredCorrectly = true;
            break; // At least one player answered correctly, no need to check further
        }
    }

    // If no one answered correctly, broadcast the correct answer to all players
    if (!answeredCorrectly)
    {
        Server.Broadcast($"Correct Answer: {currentTriviaQuestion.Answer}");
    }
}

        private void GiveReward(BasePlayer player, int rewardAmount)
        {
            foreach (var rewardItem in rewardItems)
            {
                string itemShortname = rewardItem.Key;
                int amount = rewardItem.Value + rewardAmount; // Multiply the reward amount by the configured amount

                // Use Rust's item system to give items to the player
                GiveItemToPlayer(player, itemShortname, amount);
            }
        }

        private void GiveItemToPlayer(BasePlayer player, string itemShortname, int amount)
        {
            if (amount < 1)
            {
                // Ensure that the amount is at least 1
                amount = 1;
            }

            // Find the item definition by its shortname (e.g., "wood", "stone", etc.)
            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemShortname);

            if (itemDef != null)
            {
                // Create the item with the specified amount
                Item item = ItemManager.CreateByItemID(itemDef.itemid, amount);

                // If the item creation was successful, give it to the player
                if (item != null)
                {
                    if (!player.inventory.GiveItem(item, player.inventory.containerMain))
                    {
                        item.Drop(player.transform.position, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
                        SendReply(player, $"Inventory full. {amount}x {itemShortname} dropped on the ground.");
                    }
                }
                else
                {
                    // Handle cases where item creation failed
                }
            }
            else
            {
                // Handle cases where the item definition was not found
            }
        }

        private void OnUserChat(IPlayer player, string message)
{
    if (currentTriviaQuestion != null)
    {
        if (message.Equals(currentTriviaQuestion.Answer, StringComparison.OrdinalIgnoreCase))
        {
            // The player answered correctly
            player.Message($"Correct Answer: {currentTriviaQuestion.Answer}");
            GiveReward(player.Object as BasePlayer, currentTriviaQuestion.Reward);
        }
    }
}
    }
}
