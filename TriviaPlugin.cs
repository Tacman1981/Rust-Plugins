using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Trivia Plugin", "Tacman", "1.0.0")]
    [Description("A trivia plugin for your Rust server.")]
    public class TriviaPlugin : RustPlugin
    {
        private List<TriviaQuestion> triviaQuestions = new List<TriviaQuestion>();
        private TriviaQuestion activeTriviaQuestion;
        private DateTime lastCorrectAnswerTime = DateTime.MinValue;
        private bool correctAnswerGiven = false;
        private Timer answerDisplayTimer;
        private Dictionary<string, int> rewardItems = new Dictionary<string, int>();
        private Timer triviaQuestionTimer;
        private bool isQuestionActive = false;


        private class TriviaQuestion
{
    public string Question { get; set; }
    public string Answer { get; set; }
    public int Reward { get; set; }
    public bool Answered { get; set; } // Add this line
}


        private void Init()
        {
            LoadConfig();
            LoadTriviaQuestions();
            AskTriviaQuestion();
        }

        private void LoadConfig()
        {
            rewardItems = Config.Get<Dictionary<string, int>>("RewardItems");
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
        
        private void CreateDefaultTriviaQuestions(string filePath)
{
    var defaultQuestions = new List<TriviaQuestion>
    {
        // Add your default trivia questions here
    };

    var json = JsonConvert.SerializeObject(defaultQuestions, Newtonsoft.Json.Formatting.Indented);
    File.WriteAllText(filePath, json);
    Puts("Default trivia questions file created.");
}


        private void LoadTriviaQuestions()
{
    var questionsPath = Path.Combine(Interface.Oxide.DataDirectory, "trivia_questions.json");

    if (!File.Exists(questionsPath))
    {
        CreateDefaultTriviaQuestions(questionsPath);
    }

    try
    {
        var json = File.ReadAllText(questionsPath);
        triviaQuestions = JsonConvert.DeserializeObject<List<TriviaQuestion>>(json);

        // Initialize the Answered flag for all questions
        foreach (var question in triviaQuestions)
        {
            question.Answered = false;
        }
    }
    catch (Exception ex)
    {
        Puts($"Error loading trivia questions: {ex.Message}");
    }
}

        private void AskTriviaQuestion()
        {
            if (triviaQuestions.Count == 0 || correctAnswerGiven)
            {
                // No more questions to ask or the previous question has been answered
                correctAnswerGiven = false;

                // Schedule the next question after a correct answer has been given
                if (triviaQuestionTimer != null)
                {
                    triviaQuestionTimer.Destroy();
                }

                triviaQuestionTimer = timer.Once(600, () =>
                {
                    AskTriviaQuestion();
                });

                return;
            }

            var random = new System.Random();
            activeTriviaQuestion = triviaQuestions[random.Next(triviaQuestions.Count)];

            Server.Broadcast($"Question: {activeTriviaQuestion.Question}");

            if (triviaQuestionTimer != null)
            {
                triviaQuestionTimer.Destroy();
            }

            triviaQuestionTimer = timer.Once(30, () =>
            {
                DisplayAnswer();
            });
        }

        private void DisplayAnswer()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player != null)
                {
                    player.ChatMessage($"Correct Answer: {activeTriviaQuestion.Answer}");
                }
            }

            triviaQuestions.Remove(activeTriviaQuestion);

            if (triviaQuestions.Count > 0)
            {
                AskTriviaQuestion();
            }
            else
            {
                LoadTriviaQuestions();
            }
        }

        private void OnUserChat(IPlayer player, string message)
{
    if (activeTriviaQuestion != null && !activeTriviaQuestion.Answered) // Check if a question is active and not answered
    {
        if (message.Equals(activeTriviaQuestion.Answer, StringComparison.OrdinalIgnoreCase))
        {
            Server.Broadcast($"Correct Answer: {activeTriviaQuestion.Answer}");
            GiveReward(player.Object as BasePlayer, activeTriviaQuestion.Reward);
            lastCorrectAnswerTime = DateTime.Now;

            // Set the correctAnswerGiven flag to true
            correctAnswerGiven = true;

            // Mark the question as answered
            activeTriviaQuestion.Answered = true;

            // Ask the next question or reset the list
            if (triviaQuestions.Count > 0)
            {
                AskTriviaQuestion();
            }
            else
            {
                // No more questions, reset the list or load new questions here
                LoadTriviaQuestions();
            }
        }
    }
}

        private void GiveReward(BasePlayer player, int rewardAmount)
        {
            foreach (var rewardItem in rewardItems)
            {
                string itemShortname = rewardItem.Key;
                int amount = rewardItem.Value + rewardAmount;

                GiveItemToPlayer(player, itemShortname, amount);
            }
        }

        private void GiveItemToPlayer(BasePlayer player, string itemShortname, int amount)
        {
            if (amount < 1)
            {
                amount = 1;
            }

            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemShortname);

            if (itemDef != null)
            {
                Item item = ItemManager.CreateByItemID(itemDef.itemid, amount);

                if (item != null)
                {
                    if (!player.inventory.GiveItem(item, player.inventory.containerMain))
                    {
                        item.Drop(player.transform.position, UnityEngine.Vector3.zero, UnityEngine.Quaternion.identity);
                        SendReply(player, $"Inventory full. {amount}x {itemShortname} dropped on the ground.");
                    }
                }
            }
        }
    }
}
