using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using SignalRCardMatch.Models;

namespace SignalRCardMatch.Services
{
    public interface ICardService
    {
        Task Adduser(String connectionId);
        Task AddCard(String connectionId, Card card);
        Task<Card> GetCard(String connectionId, Dictionary<String, Object> detail);
        Task SetRemoved(String connectionId, Dictionary<String, Object> detail);
        Task RemoveCard(String connectionId, Dictionary<String, Object> detail);
    }
}