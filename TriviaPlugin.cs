//not fully working, bug that stops questions needs fixing, also needs a second timer to prevent question spam after being unanswered

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
                { "scrap", 100 },
                { "ShortnameOfSecondItem", 5 }
            };
            SaveConfig();
        }
        
        private void CreateDefaultTriviaQuestions(string filePath)
{
    var defaultQuestions = new List<TriviaQuestion>
    {
        new TriviaQuestion
        {
            Question = "What is the capital of France?",
            Answer = "Paris",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many continents are there on Earth?",
            Answer = "7",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who wrote the play 'Romeo and Juliet'?",
            Answer = "William Shakespeare",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the largest planet in our solar system?",
            Answer = "Jupiter",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which gas do plants absorb from the atmosphere during photosynthesis?",
            Answer = "Carbon dioxide",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who painted the 'Mona Lisa'?",
            Answer = "Leonardo da Vinci",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the chemical symbol for gold?",
            Answer = "Au",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "In which year did the Titanic sink?",
            Answer = "1912",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the smallest prime number?",
            Answer = "2",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the largest mammal in the world?",
            Answer = "Blue whale",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who is the author of 'To Kill a Mockingbird'?",
            Answer = "Harper Lee",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the national flower of Japan?",
            Answer = "Cherry blossom",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which gas makes up the majority of Earth's atmosphere?",
            Answer = "Nitrogen",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the largest organ in the human body?",
            Answer = "Skin",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who is the current President of the United States (as of 2021)?",
            Answer = "Joe Biden",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which planet is known as the 'Red Planet'?",
            Answer = "Mars",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the longest river in the world?",
            Answer = "Nile River",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which gas do humans primarily exhale when they breathe?",
            Answer = "Carbon dioxide",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who is the author of 'Pride and Prejudice'?",
            Answer = "Jane Austen",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the chemical symbol for water?",
            Answer = "H2O",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "In which year did the Berlin Wall fall?",
            Answer = "1989",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the largest desert in the world?",
            Answer = "Antarctica",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who is the founder of Microsoft?",
            Answer = "Bill Gates",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Where would you be, if standing on the Spanish Steps?",
            Answer = "Rome",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What disease commonly spread on pirate ships?",
            Answer = "Scurvy",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who was the Ancient Greek God of the Sun?",
            Answer = "Apollo",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What year was the United Nations Established?",
            Answer = "1945",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who has won the most Total Academy Awards?",
            Answer = "Walt Disney",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many minutes are in a full week?",
            Answer = "10080",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many elements are in the periodic table?",
            Answer = "118",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many faces does a dodecahedron have?",
            Answer = "12",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many Ghosts chase Pac-Man at the start of each game?",
            Answer = "4",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What game studio makes the Red Dead Redemption series?",
            Answer = "Rockstar Games",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What planet in the Milky Way is the hottest?",
            Answer = "Venus",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the 4th letter of the Greek Alphabet?",
            Answer = "Delta",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What sports car company manufactures the 911?",
            Answer = "Porsche",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What company was initially known as Blue Ribbon Sports?",
            Answer = "Nike",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What planet has the most moons?",
            Answer = "Saturn",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "In which country would you find Mount Kilimanjaro?",
            Answer = "Tanzania",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many bones do we have in the ear?",
            Answer = "3",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What element is denoted by the chemical symbol Sn in the periodic table?",
            Answer = "Tin",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many of King Henry VIII's wives were called Catherine?",
            Answer = "3",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the currency of Denmark?",
            Answer = "Krone",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What was the old name for a Snickers Bar before it changed in 1990?",
            Answer = "Marathon",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the smallest planet in our solar system?",
            Answer = "Mercury",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which legendary artist is famous for painting melting clocks?",
            Answer = "Salvador Dali",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Name the coffee shop in the US sitcom Friends?",
            Answer = "Central Perk",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "In what year did Tony Blair become British prime minister?",
            Answer = "1997",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What does KFC stand for?",
            Answer = "Kentucky Fried Chicken",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who founded Amazon?",
            Answer = "Jeff Bezos",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is Batman's butler called?",
            Answer = "Alfred",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is a female donkey called?",
            Answer = "Jenny",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Arachnophobia is the fear of what?",
            Answer = "Spiders",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who wrote the Hunger Games?",
            Answer = "Suzanne Collins",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who is Liverpool Airport named after?",
            Answer = "John Lennon",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many consonants are there in the English alphabet?",
            Answer = "21",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Fe is the chemical symbol for which element?",
            Answer = "Iron",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What does USB stand for?",
            Answer = "Universal Serial Bus",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many chambers does the human heart have?",
            Answer = "4",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which car company created the Viper SRT10?",
            Answer = "Dodge",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which dance artist had a hit with Sky Diving?",
            Answer = "Darren Styles",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which R&B artist had a hit with In Da Club?",
            Answer = "50 Cent",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the national flower for Wales?",
            Answer = "Daffodil",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which Australian marsupial enjoys eating eucalyptus leaves?",
            Answer = "Koala",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "In nautical terms, what is the opposite of port?",
            Answer = "Starboard",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is a quarter of 1000?",
            Answer = "250",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who invented the Bikini?",
            Answer = "Louis Reard",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which ocean surrounds the Maldives?",
            Answer = "Indian Ocean",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "In J M Barrie's novel, where did the Lost Boys live?",
            Answer = "Never Never Land",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which word can be placed before Bottle, Bell & Bird?",
            Answer = "Blue",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the national flower of Japan?",
            Answer = "Cherry Blossom",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many stripes are there on the US flag?",
            Answer = "13",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many days does it take for Earth to orbit the Sun?",
            Answer = "365",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What's the capital of Canada?",
            Answer = "Ottawa",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which famous graffiti artist comes from Bristol?",
            Answer = "Banksy",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many keys does a classical piano have?",
            Answer = "88",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which city do the Beatles come from?",
            Answer = "Liverpool",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "When was Netflix founded?",
            Answer = "1997",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What was Disney's first film?",
            Answer = "Snow White & the Seven Dwarfs",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who won the X-Factor in 2011?",
            Answer = "Little Mix",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What does He stand for on the periodic table?",
            Answer = "Helium",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is a baby goat called?",
            Answer = "A kid",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many brains does an octopus have?",
            Answer = "9",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many hearts does an octopus have?",
            Answer = "3",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Where in the Human body can the smallest bone be found?",
            Answer = "Ear",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the heaviest organ in the Human body",
            Answer = "Liver",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many wives did Henry VIII have?",
            Answer = "6",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How old was Princess Diana when she died?",
            Answer = "36",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What does WWW stand for in a web browser?",
            Answer = "World Wide Web",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How long is an Olympic swimming pool in meters?",
            Answer = "50 meters",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many Languages are written from left to right?",
            Answer = "12",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What was the first soft drink in space?",
            Answer = "Coca Cola",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the fastest land animal?",
            Answer = "Cheetah",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "How many colors are there in a rainbow?",
            Answer = "7",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What is the longest bone in the Human body?",
            Answer = "Femur",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who sang, Hit Me Baby One More Time?",
            Answer = "Britney Spears",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What type of fish is Nemo",
            Answer = "Clownfish",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Who lives in a pineapple under the sea?",
            Answer = "Spongebob Squarepants",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "Which school did Harry Potter attend?",
            Answer = "Hogwarts",
            Reward = 0
        },
        new TriviaQuestion
        {
            Question = "What was the first animal to be cloned?",
            Answer = "Sheep",
            Reward = 0
        }
        // Add more questions here...
    };

    // Serialize the list of trivia questions to JSON and save it to the specified file path
    var json = JsonConvert.SerializeObject(defaultQuestions, Formatting.Indented);
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
