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
        private bool correctAnswerGiven = false;

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
                // Add your default trivia questions here
            };

            var json = JsonConvert.SerializeObject(defaultQuestions, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
            Puts("Default trivia questions file created.");
        }

        private Timer triviaQuestionTimer;
        private bool questionBroadcasted = false;

        private void AskTriviaQuestion()
        {
            if (triviaQuestions.Count == 0 || questionBroadcasted)
            {
                questionBroadcasted = false;
                return;
            }

            var random = new System.Random();
            currentTriviaQuestion = triviaQuestions[random.Next(triviaQuestions.Count)];

            Server.Broadcast($"Question: {currentTriviaQuestion.Question}");

            questionBroadcasted = true;

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
            if (!correctAnswerGiven)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player != null)
                    {
                        player.ChatMessage($"Correct Answer: {currentTriviaQuestion.Answer}");
                    }
                }

                correctAnswerGiven = true;
                lastCorrectAnswerTime = DateTime.Now;
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

        private void OnUserChat(IPlayer player, string message)
        {
            if (currentTriviaQuestion != null)
            {
                if (message.Equals(currentTriviaQuestion.Answer, StringComparison.OrdinalIgnoreCase))
                {
                    DateTime currentTime = DateTime.Now;

                    if (!correctAnswerGiven || (currentTime - lastCorrectAnswerTime).TotalSeconds > 10.0)
                    {
                        player.Message($"Correct Answer: {currentTriviaQuestion.Answer}");
                        GiveReward(player.Object as BasePlayer, currentTriviaQuestion.Reward);
                        correctAnswerGiven = true;
                        lastCorrectAnswerTime = currentTime;
                    }
                    else
                    {
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
                int amount = rewardItem.Value + rewardAmount;

                GiveItemToPlayer(player, itemShortname, amount);
            }
        }
    }
}
