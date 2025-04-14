using System.Data;
using System.Text.Json;
using System.Net;

namespace MagicCardInventory
{
    #region Class
    public class Inventory
    {
        #region Member Variables
        private static readonly SQL sql = new SQL("Server=localhost;Database=Inventory;Trusted_Connection=True;");
        private static readonly HttpClient client = new HttpClient();
        private static readonly string multiLayouts = "split,flip,transform,modal_dfc,reversible_card";
        private static string print_foil_str = string.Empty;
        #endregion

        #region Main
        /// <summary>
        /// Main method
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static async Task Main(string[] args)
        {
            short count = 1;
            if (args.Length > 4 && !string.IsNullOrWhiteSpace(args[4]) && !short.TryParse(args[4], out count)) 
            {
                Console.WriteLine("Invalid value for count.");
                return;
            }

            client.BaseAddress = new Uri("https://api.scryfall.com/cards/");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MagicCardInventory") ;
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            //Begin a new SQL transaction
            sql.BeginTransaction();

            //Do what we want to do
            switch(args[0].ToLower())
            {
                case "add":
                    await InventoryCard(args[1], args[2], args[3], count);
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
        private static async Task InventoryCard(string p_name, string p_set, string p_foil_str, short p_count)
        {

            /* Declare variables needed for card inventory */
            decimal price = 0M;
            string cardName = string.Empty;
            string setName = string.Empty;
            string type = string.Empty;
            string scryfallId = string.Empty;
            string rarity = string.Empty;
            bool blueColor = false;
            bool blackColor = false;
            bool redColor = false;
            bool greenColor = false;
            bool whiteColor = false;
            short uncoloredMana = 0;
            short blueMana = 0;
            short blackMana = 0;
            short redMana = 0;
            short greenMana = 0;
            short whiteMana = 0;
            short sequence = 0;
            bool hybrid = false;
            bool foil = p_foil_str.ToLower() == "yes";
            if (foil) print_foil_str = " - FOIL";
            p_name = p_name.Replace(" ", "+");

            /* Get card data from API call */
            string responseJson = await client.GetStringAsync(client.BaseAddress + "named?exact=" + p_name + "&set=" + p_set);
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

            /* Get the type from the card data */
            if (!string.IsNullOrWhiteSpace(card.TypeLine)) type = card.TypeLine.ToUpper();
            else throw new Exception("No type returned");

            /* Get the scryfall id from the card data */
            if (!string.IsNullOrWhiteSpace(card.Id)) scryfallId = card.Id;
            else throw new Exception("No scryfall id returned");

            /* Get the rarity from the card data */
            if (!string.IsNullOrWhiteSpace(card.Rarity))
            {
                rarity = (card?.Rarity.ToLower()) switch
                {
                    "common" => "C",
                    "uncommon" => "U",
                    "rare" => "R",
                    "mythic" => "M",
                    "special" => "S",
                    "bonus" => "B",
                    _ => "Z",
                };
            }
            else throw new Exception("No rarity returned");

            /* Get the colors from the card data */
            if (card?.Colors is not null && card.Colors.Count > 0)
            {
                if (card.Colors.Contains("U")) blueColor = true;
                if (card.Colors.Contains("B")) blackColor = true;
                if (card.Colors.Contains("R")) redColor = true;
                if (card.Colors.Contains("G")) greenColor = true;
                if (card.Colors.Contains("W")) whiteColor = true;
            }

            /* Get the mana cost from the card data */
            if (!string.IsNullOrWhiteSpace(card?.ManaCost))
            {
                if (short.TryParse(card.ManaCost.Substring(card.ManaCost.IndexOf("{") + 1, card.ManaCost.IndexOf("}") - card.ManaCost.IndexOf("{") - 1), out short uncoloredMana_temp))
                    uncoloredMana = uncoloredMana_temp;
                blueMana = (short)card.ManaCost.Count(f => f == 'U');
                blackMana = (short)card.ManaCost.Count(f => f == 'B');
                redMana = (short)card.ManaCost.Count(f => f == 'R');
                greenMana = (short)card.ManaCost.Count(f => f == 'G');
                whiteMana = (short)card.ManaCost.Count(f => f == 'W');
                if (card.ManaCost.Contains('/')) hybrid = true;
            }

            /* Check if we already have this card inventoried and just need to increase count (and update price because why not) */
            if (CheckExists(cardName, setName, foil, out int cardId))
            {
                //Update count on existing entry in database
                if (sql.UpdateCardCount(cardId, p_count) != 1) throw new Exception("Error updating card count");

                //Update price for card
                if (sql.UpdateCardPrice(cardId, price) != 1) throw new Exception("Error updating card price");

                //Print card that we updates
                Console.WriteLine("Card count and price updated: " + cardName + " - " + setName + print_foil_str + " - " + price + ", count: " + p_count);
            }
            else
            {
                //Add new card
                if (sql.InsertCardInfo(cardId, scryfallId, cardName, setName, type, rarity, foil) != 1) throw new Exception("Error inserting new card into tblCardInfo");
                if (sql.InsertCardPrice(cardId, price) != 1) throw new Exception("Error inserting new card into tblCardPrice");
                if (sql.InsertCardCount(cardId, p_count) != 1) throw new Exception("Error inserting new card into tblCardCount");

                /* If there are multiple card faces, get information from the card faces object and insert rows for colors and mana cost for each of them */
                //Eligible layouts are the following:
                    //split
                    //flip
                    //transform
                    //modal_dfc
                    //reversible_card

                if (!string.IsNullOrWhiteSpace(card?.Layout) && multiLayouts.IndexOf(card.Layout.ToLower()) > -1 && card.CardFaces is not null)
                {
                    foreach (CardFace cf in card.CardFaces)
                    {
                        bool blueColor_cf = blueColor;
                        bool blackColor_cf = blackColor;
                        bool redColor_cf = redColor;
                        bool greenColor_cf = greenColor;
                        bool whiteColor_cf = whiteColor;

                        short uncoloredMana_cf = uncoloredMana;
                        short blueMana_cf = blueMana;
                        short blackMana_cf = blackMana;
                        short redMana_cf = redMana;
                        short greenMana_cf = greenMana;
                        short whiteMana_cf = whiteMana;
                        bool hybrid_cf = hybrid;

                        //Get the colors from the card face object
                        if (cf.Colors is not null && cf.Colors.Count > 0)
                        {
                            if (cf.Colors.Contains("U")) blueColor_cf = true;
                            if (cf.Colors.Contains("B")) blackColor_cf = true;
                            if (cf.Colors.Contains("R")) redColor_cf = true;
                            if (cf.Colors.Contains("G")) greenColor_cf = true;
                            if (cf.Colors.Contains("W")) whiteColor_cf = true;
                        }

                        //Get the mana cost from the card face object
                        if (!string.IsNullOrWhiteSpace(cf.ManaCost))
                        {
                            if (short.TryParse(cf.ManaCost.Substring(cf.ManaCost.IndexOf("{") + 1, cf.ManaCost.IndexOf("}") - cf.ManaCost.IndexOf("{") - 1), out short uncoloredMana_cf_temp))
                                uncoloredMana_cf = uncoloredMana_cf_temp;
                            blueMana_cf = (short)cf.ManaCost.Count(f => f == 'U');
                            blackMana_cf = (short)cf.ManaCost.Count(f => f == 'B');
                            redMana_cf = (short)cf.ManaCost.Count(f => f == 'R');
                            greenMana_cf = (short)cf.ManaCost.Count(f => f == 'G');
                            whiteMana_cf = (short)cf.ManaCost.Count(f => f == 'W');
                            if (cf.ManaCost.Contains('/')) hybrid_cf = true;
                        }

                        //Insert into tblCardColors and tblCardManaCost
                        if (sql.InsertCardColors(cardId, sequence, blueColor_cf, blackColor_cf, redColor_cf, greenColor_cf, whiteColor_cf) != 1) throw new Exception("Error inserting new card into tblCardColors");
                        if (sql.InsertCardManaCost(cardId, sequence, uncoloredMana_cf, blueMana_cf, blackMana_cf, redMana_cf, greenMana_cf, whiteMana_cf, hybrid_cf) != 1) throw new Exception("Error inserting new card into tblCardManaCost");

                        sequence++;
                    }
                }
                else
                {
                    //Insert non-multi layout into tblCardColors and tblCardManaCost
                    if (sql.InsertCardColors(cardId, sequence, blueColor, blackColor, redColor, greenColor, whiteColor) != 1) throw new Exception("Error inserting new card into tblCardColors");
                    if (sql.InsertCardManaCost(cardId, sequence, uncoloredMana, blueMana, blackMana, redMana, greenMana, whiteMana, hybrid) != 1) throw new Exception("Error inserting new card into tblCardManaCost");
                }

                //Print card that we added
                Console.WriteLine("New card added: " + cardName + " - " + setName + print_foil_str + " - " + price + ", count: " + p_count);
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
                sql.ClearParameters();
                string strAddress = client.BaseAddress + row.Field<string>("scryfall_id_str");
                /* Get card data from API call */
                try
                {
                    responseJson = await client.GetStringAsync(strAddress);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to get info for card: " + strAddress + ", Error: " + ex.ToString());
                }

                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    throw new Exception("Unable to get card data from API call for card: " + strAddress);
                }

                card = JsonSerializer.Deserialize<Card>(responseJson);

                if (card is null) throw new Exception("Unable to deserialize JSON into card object");

                /* Get the price from the card data */
                string? price_str = card.Prices?.Usd;
                if (row.Field<bool>("foil_bool"))
                {
                    price_str = card.Prices?.UsdFoil;
                    print_foil_str = " - FOIL";
                }

                if (!string.IsNullOrWhiteSpace(price_str)) price = Convert.ToDecimal(price_str);
                else throw new Exception("No price information returned");

                /* Update price for the card */
                if (sql.UpdateCardPrice(row.Field<int>("card_id_int"), price) != 1) throw new Exception("Error updating card price");

                /* Print what card we just updated */
                Console.WriteLine("Price updated: " + row.Field<string>("card_name_str") + " - " + row.Field<string>("set_str") + print_foil_str + " - " + price);
                print_foil_str = string.Empty;

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
                if (p_cardId == 0) throw new Exception("Unable to get next key for type CARDID");
                if (sql.UpdateNextKey("CARDID") != 1) throw new Exception("Error updating next key type CARDID");
                return false;
            }
            return true;
        }
        #endregion
    }
    #endregion
}