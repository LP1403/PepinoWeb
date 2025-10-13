using GameServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("cards")]
        public IActionResult TestCards()
        {
            try
            {
                Console.WriteLine("🧪 Endpoint de prueba de cartas llamado");
                CardService.TestCardGeneration();
                
                var deck = CardService.CreateSpanishDeck();
                var shuffled = CardService.ShuffleDeck(deck);
                var (hands, remaining) = CardService.DealAllCards(shuffled, 2);
                
                var result = new
                {
                    DeckCount = deck.Count,
                    ShuffledCount = shuffled.Count,
                    Hands = hands.Select((hand, index) => new
                    {
                        PlayerIndex = index,
                        CardCount = hand.Count,
                        SampleCards = hand.Take(3).Select(c => $"{c.Value}{c.Suit}").ToList()
                    }).ToList(),
                    RemainingCount = remaining.Count
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en prueba de cartas: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 