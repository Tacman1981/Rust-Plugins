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
        private DateTime lastCorrectAnswerTime = DateTime.MinValue;

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
        private bool questionBroadcasted = false; // Add a flag to track if the question has been broadcasted

        private void AskTriviaQuestion()
{
    if (triviaQuestions.Count == 0 || questionBroadcasted)
        return; // No more questions to ask or question already broadcasted

    // Select a random question
    var random = new System.Random();
    currentTriviaQuestion = triviaQuestions[random.Next(triviaQuestions.Count)];

    // Broadcast the question to all online players
    Server.Broadcast($"Question: {currentTriviaQuestion.Question}");

    // Set the flag to true to indicate that the question has been broadcasted
    questionBroadcasted = true;

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
    if (!correctAnswerDisplayed)
    {
        float currentTime = UnityEngine.Time.realtimeSinceStartup; // Get the current time using UnityEngine.Time
        DateTime currentDateTime = DateTime.Now; // Get the current date and time

        foreach (var player in BasePlayer.activePlayerList)
        {
            if (player != null)
            {
                player.ChatMessage($"Correct Answer: {currentTriviaQuestion.Answer}");
            }
        }

        correctAnswerDisplayed = true; // Set the flag to indicate the correct answer has been displayed
        lastCorrectAnswerTime = currentDateTime; // Record the time of the correct answer as a DateTime

        // Call the GiveReward method here if it was missing in your code
        // GiveReward(player.Object as BasePlayer, currentTriviaQuestion.Reward);
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

        private bool correctAnswerDisplayed = false; // Add a flag to track if the correct answer has been displayed

private void OnUserChat(IPlayer player, string message)
{
    if (currentTriviaQuestion != null)
    {
        if (message.Equals(currentTriviaQuestion.Answer, StringComparison.OrdinalIgnoreCase))
        {
            DateTime currentTime = DateTime.Now; // Get the current time

            if (!correctAnswerDisplayed || (currentTime - lastCorrectAnswerTime).TotalSeconds > 10.0)
            {
                player.Message($"Correct Answer: {currentTriviaQuestion.Answer}");
                GiveReward(player.Object as BasePlayer, currentTriviaQuestion.Reward);
                correctAnswerDisplayed = true; // Set the flag to indicate the correct answer has been displayed
                lastCorrectAnswerTime = currentTime; // Record the time of the correct answer
            }
            else
            {
                // The player repeated the correct answer within 10 seconds
                player.Message("The question was already answered.");
            }
        }
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

}
}
