﻿using System.Data;
using System.Text.Json;

namespace MagicCardInventory
{
    #region Class
    public class Inventory
    {
        #region Member Variables
        private static readonly SQL sql = new("Server=localhost;Database=Inventory;Trusted_Connection=True;");
        private static readonly HttpClient client = new();
        #endregion

        #region Main
        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static async Task Main(string[] args)
        {
            //Begin a new SQL transaction
            sql.BeginTransaction();

            //Do what we want to do
            switch(args[0].ToLower())
            {
                case "add":
                    await InventoryCard(args[1], args[2], args[3]);
                    break;
                case "updateprices":
                    await UpdatePrices();               
                    break;
                default:
                    Console.WriteLine("Invalid operation. Valid operations are 'add' and 'updateprices'.");
                    break;
            }

            //Commit SQL transaction
            if (!sql.CommitTransaction()) sql.RollbackTransaction();

        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Adds a card to the inventory
        /// </summary>
        /// <param name="p_name">Name of the card</param>
        /// <param name="p_set">Set of the card</param>
        /// <param name="p_foil_str">True if the card is a foil</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static async Task InventoryCard(string p_name, string p_set, string p_foil_str)
        {

            /* Declare variables needed for card inventory */
            decimal price = 0M;
            string cardName = string.Empty;
            string setName = string.Empty;
            string scryfallId = string.Empty;
            string rarity = string.Empty;
            bool foil = p_foil_str.ToLower() == "yes";

            /* Get card data from API call */
            string responseJson = await client.GetStringAsync("https://api.scryfall.com/cards/named?exact=" + p_name + "&set=" + p_set);
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                throw new Exception("Unable to get card data from API call for card: " + p_name + " , set: " + p_set);
            }

            Card? card = JsonSerializer.Deserialize<Card>(responseJson);

            if (card is null) throw new Exception("Unable to deserialize JSON into card object");

            /* Get the price from the card data */
            string? price_str = card.Prices?.Usd;
            if (foil) price_str = card.Prices?.UsdFoil;

            if (!string.IsNullOrWhiteSpace(price_str)) price = Convert.ToDecimal(price_str);
            else throw new Exception("No price information returned");

            /* Get the card name from the card data */
            if (!string.IsNullOrWhiteSpace(card.Name)) cardName = card.Name.ToUpper();
            else throw new Exception("No card name returned");

            /* Get the set name from the card data */
            if (!string.IsNullOrWhiteSpace(card.SetName)) setName = card.SetName.ToUpper();
            else throw new Exception("No set name returned");

            /* Get the scryfall id from the card data */
            if (!string.IsNullOrWhiteSpace(card.Id)) scryfallId = card.Id;
            else throw new Exception("No scryfall id returned");

            /* Get the rarity from the card data */
            if (!string.IsNullOrWhiteSpace(card.Rarity))
            {
                switch(card?.Rarity.ToLower())
                {
                    case "common":
                        rarity = "C";
                        break;
                    case "uncommon":
                        rarity = "U";
                        break;
                    case "rare":
                        rarity = "R";
                        break;
                    case "mythic":
                        rarity = "M";
                        break;
                    case "special":
                        rarity = "S";
                        break;
                    case "bonus":
                        rarity = "B";
                        break;
                    default:
                        rarity = "Z";
                        break;                            
                }
            }
            else throw new Exception("No rarity returned");

            /* Check if we already have this card inventoried and just need to increase count (and update price because why not) */
            if (CheckExists(cardName, setName, foil, out int cardId))
            {
                //Update count on existing entry in database
                if (sql.UpdateCardCount(cardId) != 1) throw new Exception("Error updating card count");

                //Update price for card
                if (sql.UpdateCardPrice(cardId, price) != 1) throw new Exception("Error updating card price");
            }
            else
            {
                //Add new card
                if (sql.InsertCardInfo(cardId, scryfallId, cardName, setName, rarity, foil) != 1) throw new Exception("Error inserting new card into tblCardInfo");
                if (sql.InsertCardPrice(cardId, price) != 1) throw new Exception("Error inserting new card into tblCardPrice");
                if (sql.InsertCardCount(cardId) != 1) throw new Exception("Error inserting new card into tblCardCount");
            }
        }

        /// <summary>
        /// Updates prices of all cards inventoried thus far
        /// </summary>
        private static async Task UpdatePrices()
        {
            /* Get all cards in the database */
            DataTable cards = sql.GetAllCards();
            if (cards.Rows.Count == 0) return;
            
            string responseJson = String.Empty;
            Card? card = null;
            decimal price = 0M;

            /* Loop through and 1) get price from API and 2) update price in the database */
            foreach (DataRow row in cards.Rows)
            {
                /* Get card data from API call */
                responseJson = await client.GetStringAsync("https://api.scryfall.com/cards/" + row.Field<string>("scryfall_id_str"));
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    throw new Exception("Unable to get card data from API call for card: " + row.Field<string>("scryfall_id_str"));
                }

                card = JsonSerializer.Deserialize<Card>(responseJson);

                if (card is null) throw new Exception("Unable to deserialize JSON into card object");

                /* Get the price from the card data */
                string? price_str = card.Prices?.Usd;
                if (row.Field<bool>("foil_bool")) price_str = card.Prices?.UsdFoil;

                if (!string.IsNullOrWhiteSpace(price_str)) price = Convert.ToDecimal(price_str);
                else throw new Exception("No price information returned");

                /* Update price for the card */
                if (sql.UpdateCardPrice(row.Field<int>("card_id_int"), price) != 1) throw new Exception("Error updating card price");

                //Wait 100 milliseconds per API rate limit
                await Task.Delay(100);
            }          
        }

        /// <summary>
        /// Checks if the card already exists in the database
        /// </summary>
        /// <param name="p_cardName">Name of the card</param>
        /// <param name="p_set">Set of the card</param>
        /// <param name="p_foil">True if the card is foil</param>
        /// <param name="p_cardId">Card ID of the existing card, or a new card ID if the card does not exist yet</param>
        /// <returns>True if the card exists, false otherwise</returns>
        private static bool CheckExists(string p_cardName, string p_set, bool p_foil, out int p_cardId)
        {
            p_cardId = sql.GetCardId(p_cardName, p_set, p_foil);
            if (p_cardId == 0)
            {
                p_cardId = sql.GetNextKey("CARDID");
                if (sql.UpdateNextKey("CARDID") != 1) throw new Exception("Error updating next key type CARDID");
                return false;
            }
            return true;
        }

        #endregion

    }
    #endregion
}