using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using SignalRCardMatch.Models;
using SignalRCardMatch.Services;

namespace SignalRCardMatch.Hubs
{
    public class GameHub : Hub
    {
        private readonly ICardService _cardService;

        public GameHub(ICardService cardService)
        {
            _cardService = cardService;
        }

        public async Task Request(Object reqObject)
        {
            using var reqJson = JsonDocument.Parse(reqObject.ToString());
            var root = reqJson.RootElement;

            if (Context.Items[Context.ConnectionId] == null)
            {
                Context.Items.Add(Context.ConnectionId, new Dictionary<String, Object>());
            }
            var userItems = Context.Items[Context.ConnectionId] as Dictionary<String, Object>;

            Console.WriteLine(root.ToString());

            var response = new ResponseJson
            {
                Event = "",
                Target = "",
                Options = null
            };

            switch (root.GetProperty("event").GetString())
            {
                case "NewGame":
                    await NewGame(userItems, response);
                    break;
                case "CardClicked":
                    if (userItems["status"].ToString() == "userTurn")
                    {
                        await CardClicked(root.GetProperty("options").GetProperty("cardDetail"), response);
                    }
                    break;
                case "ShowCardsEnd":
                    if (Convert.ToInt32(userItems["AIMatchCount"]) + Convert.ToInt32(userItems["userMatchCount"]) >= 8)
                    {
                        response.Event = "end";
                        if (Convert.ToInt32(userItems["AIMatchCount"]) > Convert.ToInt32(userItems["userMatchCount"]))
                        {
                            response.Options = new Dictionary<string, object>
                            {
                                ["banner"] = "AI Win!"
                            };
                        }
                        else if (Convert.ToInt32(userItems["AIMatchCount"]) < Convert.ToInt32(userItems["userMatchCount"]))
                        {
                            response.Options = new Dictionary<string, object>
                            {
                                ["banner"] = "You Win!"
                            };
                        }
                        else
                        {
                            response.Options = new Dictionary<string, object>
                            {
                                ["banner"] = "Even!"
                            };
                        }
                        userItems["status"] = "end";
                        
                        await Clients.Caller.SendAsync("Response", JsonSerializer.Serialize(response, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }));
                    }
                    else if (Convert.ToInt32(userItems["selectedCardsCount"]) == 0 && Convert.ToInt32(userItems["AIState"]) < 2)
                    {
                        if (userItems["status"].ToString() == "userTurn")
                        {
                            Thread.Sleep(100);

                            response.Event = "AITurn";
                            userItems["status"] = "AITurn";
                            userItems["AIState"] = 0;

                            response.Event = "AITurn";
                            response.Options = new Dictionary<string, object>
                            {
                                ["banner"] = "AI turn"
                            };

                            await Clients.Caller.SendAsync("Response", JsonSerializer.Serialize(response, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            }));
                        }

                        Thread.Sleep(1000);

                        if (Convert.ToInt32(userItems["AIState"]) == 0)
                        {
                            Random rand = new Random(Guid.NewGuid().GetHashCode());
                            var currentMap = userItems["currentMap"] as int[];
                            int idx = -1;
                            Card _card = await _cardService.GetCard(Context.ConnectionId, new Dictionary<string, object>
                            {
                                ["y"] = idx / 4,
                                ["x"] = idx % 4
                            });
                            bool flag = true;
                            while (flag)
                            {
                                idx = rand.Next(0, 16);
                                _card = await _cardService.GetCard(Context.ConnectionId, new Dictionary<string, object>
                                {
                                    ["y"] = idx / 4,
                                    ["x"] = idx % 4
                                });

                                if (idx != -1 && Convert.ToBoolean(_card.detail["removed"]) == false)
                                {
                                    flag = false;
                                }
                            }

                            response.Event = "ShowCards";
                            response.Options = new Dictionary<string, object>
                            {
                                ["cards"] = new List<Card> {
                                    _card
                                },
                                ["banner"] = "Wait AI Choose another"
                            };

                            userItems["tmpCard"] = _card;
                            userItems["AIState"] = 1;
                        }
                        else if (Convert.ToInt32(userItems["AIState"]) == 1)
                        {
                            Random rand = new Random(Guid.NewGuid().GetHashCode());
                            var currentMap = userItems["currentMap"] as int[];
                            int idx = -1;
                            Card _card = await _cardService.GetCard(Context.ConnectionId, new Dictionary<string, object>
                            {
                                ["y"] = idx / 4,
                                ["x"] = idx % 4
                            });
                            var lastCard = userItems["tmpCard"] as Card;
                            bool flag = true;
                            while (flag)
                            {
                                idx = rand.Next(0, 16);
                                _card = await _cardService.GetCard(Context.ConnectionId, new Dictionary<string, object>
                                {
                                    ["y"] = idx / 4,
                                    ["x"] = idx % 4
                                });

                                if (idx != -1 && Convert.ToBoolean(_card.detail["removed"]) == false && (Convert.ToInt32(lastCard.detail["x"]) + Convert.ToInt32(lastCard.detail["y"]) * 4 != idx))
                                {
                                    flag = false;
                                }
                            }

                            if (_card.value == lastCard.value)
                            {
                                response.Event = "ShowCards";
                                response.Options = new Dictionary<string, object>
                                {
                                    ["cards"] = new List<Card> {
                                        _card
                                    },
                                    ["banner"] = "AI: Match!"
                                };
                                await _cardService.SetRemoved(Context.ConnectionId, lastCard.detail);
                                await _cardService.SetRemoved(Context.ConnectionId, _card.detail);
                                userItems["AIMatchCount"] = Convert.ToInt32(userItems["AIMatchCount"]) + 1;
                            }
                            else
                            {
                                response.Event = "ShowCardsThenHide";
                                response.Options = new Dictionary<string, object>
                                {
                                    ["cards"] = new List<Card> {
                                        _card,
                                        lastCard
                                    },
                                    ["banner"] = "AI: oh no!"
                                };
                            }

                            await Clients.Caller.SendAsync("Response", JsonSerializer.Serialize(response, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            }));

                            Thread.Sleep(1000);

                            response.Event = "userTurn";
                            response.Options = new Dictionary<string, object>
                            {
                                ["banner"] = "User turn"
                            };

                            userItems["status"] = "userTurn";
                            userItems["AIState"] = 2;
                        }
                    }
                    break;
                default:
                    break;
            }

            var responseJsonString = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (root.GetProperty("event").GetString() == "ShowCardsEnd" && Convert.ToInt32(userItems["AIMatchCount"]) + Convert.ToInt32(userItems["userMatchCount"]) >= 8) return;
            await Clients.Caller.SendAsync("Response", responseJsonString);
        }

        private async Task NewGame(dynamic userItems, dynamic response)
        {
            Console.WriteLine(Context.ConnectionId);
            int[] cardMap = new int[] {
                12,12,25,25,
                38,38,51,51,
                0,0,13,13,
                26,26,39,39
            };

            await _cardService.Adduser(Context.ConnectionId);
            // Context.Items.Add(Context.ConnectionId, new Dictionary<String, Object>());
            // var userItems = Context.Items[Context.ConnectionId] as Dictionary<String, Object>;
            userItems.Add("status", "");
            userItems.Add("selectedCardsCount", 0);
            userItems.Add("tmpCard", null);
            userItems.Add("AIState", 0);
            userItems.Add("userMatchCount", 0);
            userItems.Add("AIMatchCount", 0);
            userItems.Add("currentMap", new int[]{
                -1,-1,-1,-1,
                -1,-1,-1,-1,
                -1,-1,-1,-1,
                -1,-1,-1,-1,
            });

            Random rand = new Random(Guid.NewGuid().GetHashCode());
            for (int i = 0; i < cardMap.Length; i++)
            {
                int tmp = cardMap[i];
                int r = rand.Next(0, cardMap.Length);
                cardMap[i] = cardMap[r];
                cardMap[r] = tmp;
            }

            for (int i = 0; i < cardMap.Length; i++)
            {
                Card card = new Card
                {
                    value = cardMap[i],
                    detail = new Dictionary<String, Object>
                    {
                        ["y"] = i / 4,
                        ["x"] = i % 4,
                        ["removed"] = false
                    }
                };
                Console.WriteLine($"y: {i / 4} x: {i % 4} val: {cardMap[i]}");

                await _cardService.AddCard(Context.ConnectionId, card);
            }

            response.Event = "InitializeCards";
            userItems["status"] = "userTurn";
        }

        private async Task CardClicked(JsonElement detail, dynamic response)
        {
            Console.WriteLine(Context.ConnectionId);
            Console.WriteLine(detail.ToString());

            var clickedCard = await _cardService.GetCard(Context.ConnectionId, new Dictionary<String, Object>
            {
                ["y"] = detail.GetProperty("y").GetInt32(),
                ["x"] = detail.GetProperty("x").GetInt32()
            });

            var userItems = Context.Items[Context.ConnectionId] as Dictionary<String, Object>;

            var currentMap = userItems["currentMap"] as int[];
            currentMap[Convert.ToInt32(clickedCard.detail["y"]) * 4 + Convert.ToInt32(clickedCard.detail["x"])] = clickedCard.value;

            if (Convert.ToInt32(userItems["selectedCardsCount"]) == 1)
            {
                var lastCard = userItems["tmpCard"] as Card;

                if (lastCard.value == clickedCard.value)
                {
                    // await Clients.Caller.SendAsync("ShowCard", clickedCard);
                    response.Event = "ShowCards";
                    response.Options = new Dictionary<string, object>
                    {
                        ["cards"] = new List<Card> {
                            clickedCard
                        },
                        ["banner"] = "Match!"
                    };
                    await _cardService.SetRemoved(Context.ConnectionId, lastCard.detail);
                    await _cardService.SetRemoved(Context.ConnectionId, clickedCard.detail);
                    userItems["userMatchCount"] = Convert.ToInt32(userItems["userMatchCount"]) + 1;
                }
                else
                {
                    // await Clients.Caller.SendAsync("ShowCardThenHide", clickedCard, lastCard);
                    response.Event = "ShowCardsThenHide";
                    response.Options = new Dictionary<string, object>
                    {
                        ["cards"] = new List<Card> {
                            clickedCard,
                            lastCard
                        },
                        ["banner"] = "Oh no!"
                    };
                }

                userItems["selectedCardsCount"] = 0;
                userItems["AIState"] = 0;
            }
            else if (Convert.ToInt32(userItems["selectedCardsCount"]) == 0)
            {
                userItems["tmpCard"] = clickedCard;
                userItems["selectedCardsCount"] = 1;

                // await Clients.Caller.SendAsync("ShowCard", clickedCard);
                response.Event = "ShowCards";
                response.Options = new Dictionary<string, object>
                {
                    ["cards"] = new List<Card> {
                        clickedCard
                    },
                    ["banner"] = "Choose another"
                };
            }
        }

        public async Task Test()
        {
            await Clients.Caller.SendAsync("Test", "~~~");
        }

        // public async Task NewGame()
        // {
        //     Console.WriteLine(Context.ConnectionId);
        //     int[] cardMap = new int[] {
        //         12,12,25,25,
        //         38,38,51,51,
        //         0,0,13,13,
        //         26,26,39,39
        //     };

        //     await _cardService.Adduser(Context.ConnectionId);
        //     Context.Items.Add(Context.ConnectionId, new Dictionary<String, Object>());
        //     var userItems = Context.Items[Context.ConnectionId] as Dictionary<String, Object>;
        //     userItems.Add("status", "");
        //     userItems.Add("selectedCardsCount", 0);
        //     userItems.Add("tmpCard", null);

        //     Random rand = new Random(Guid.NewGuid().GetHashCode());
        //     for (int i = 0; i < cardMap.Length; i++)
        //     {
        //         int tmp = cardMap[i];
        //         int r = rand.Next(0, cardMap.Length);
        //         cardMap[i] = cardMap[r];
        //         cardMap[r] = tmp;
        //     }

        //     for (int i = 0; i < cardMap.Length; i++)
        //     {
        //         Card card = new Card
        //         {
        //             value = cardMap[i],
        //             detail = new Dictionary<String, Object>
        //             {
        //                 ["y"] = i / 4,
        //                 ["x"] = i % 4
        //             }
        //         };
        //         Console.WriteLine($"y: {i / 4} x: {i % 4} val: {cardMap[i]}");

        //         await _cardService.AddCard(Context.ConnectionId, card);
        //     }

        //     await Clients.Caller.SendAsync("InitializeGame");
        // }

        // public async Task CardClicked(Object detail)
        // {
        //     Console.WriteLine(Context.ConnectionId);
        //     Console.WriteLine(detail.ToString());

        //     using var doc = JsonDocument.Parse(detail.ToString());
        //     var root = doc.RootElement;

        //     var clickedCard = await _cardService.GetCard(Context.ConnectionId, new Dictionary<String, Object>
        //     {
        //         ["y"] = root.GetProperty("y").GetInt32(),
        //         ["x"] = root.GetProperty("x").GetInt32()
        //     });

        //     var userItems = Context.Items[Context.ConnectionId] as Dictionary<String, Object>;
        //     if (Convert.ToInt32(userItems["selectedCardsCount"]) == 1)
        //     {
        //         var lastCard = userItems["tmpCard"] as Card;

        //         if (lastCard.value == clickedCard.value)
        //         {
        //             await Clients.Caller.SendAsync("ShowCard", clickedCard);
        //         }
        //         else
        //         {
        //             await Clients.Caller.SendAsync("ShowCardThenHide", clickedCard, lastCard);
        //         }

        //         userItems["selectedCardsCount"] = 0;
        //     }
        //     else if (Convert.ToInt32(userItems["selectedCardsCount"]) == 0)
        //     {
        //         userItems["tmpCard"] = clickedCard;
        //         userItems["selectedCardsCount"] = 1;

        //         await Clients.Caller.SendAsync("ShowCard", clickedCard);
        //     }
        // }
    }
}