using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SignalRCardMatch.Models;

namespace SignalRCardMatch.Services {
    public class CardMatchService : ICardService
    {
        private readonly Dictionary<String, List<Card>> user_cards;

        public CardMatchService()
        {
            user_cards = new Dictionary<String, List<Card>>();
        }

        public async Task Adduser(String connectionId) {
            await Task.Run(() => user_cards.Add(connectionId, new List<Card>()));
        }

        public async Task AddCard(String connectionId, Card card) {
            await Task.Run(() => user_cards[connectionId].Add(card));
        }

        public async Task<Card> GetCard(String connectionId, Dictionary<String, Object> detail) {
            return await Task.Run(() => user_cards[connectionId].FirstOrDefault((item) => item.detail["x"].Equals(detail["x"]) && item.detail["y"].Equals(detail["y"])));
        }

        public async Task SetRemoved(String connectionId, Dictionary<String, Object> detail) {
            var result = user_cards[connectionId].FirstOrDefault((item) => item.detail["x"].Equals(detail["x"]) && item.detail["y"].Equals(detail["y"]));
            if(result != null) {
                await RemoveCard(connectionId, result.detail);
                result.detail["removed"] = true;
                await Task.Run(() => user_cards[connectionId].Add(result));
            }
        }

        public async Task RemoveCard(String connectionId, Dictionary<String, Object> detail) {
            var result = user_cards[connectionId].FirstOrDefault((item) => item.detail["x"].Equals(detail["x"]) && item.detail["y"].Equals(detail["y"]));
            if(result != null) {
                await Task.Run(() => user_cards[connectionId].Remove(result));
            }
        }
    }
}