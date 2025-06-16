using System.Data;
using System.Data.SqlClient;
using BaseClasses;

namespace MagicCardInventory
{
    internal class SQL : SqlBase
    {
        #region Constructors
        internal SQL() : base() { }
        internal SQL(string connectionString) : base(connectionString) { }
        #endregion

        #region Selects
        internal DataTable GetAllCards()
        {
            ClearParameters();
            Command.CommandText = "SELECT c.card_id_int, c.scryfall_id_str, c.card_name_str, c.set_str, c.foil_bool FROM tblCardInfo c";
            SqlDataReader reader = Command.ExecuteReader();
            DataTable results = new DataTable();
            results.Load(reader);
            return results;
        }
        internal int GetCardId(string p_cardName, string p_set, bool p_foil)
        {
            ClearParameters();
            /* Build SQL query */
            Command.CommandText = "SELECT c.card_id_int FROM tblCardInfo c WHERE c.card_name_str = @p_cardName AND c.set_str = @p_set AND c.foil_bool = @p_foil";
            Command.Parameters.AddWithValue("@p_cardName", p_cardName);
            Command.Parameters.AddWithValue("@p_set", p_set);
            Command.Parameters.AddWithValue("@p_foil", p_foil);

            /* Execute query */
            SqlDataReader reader = Command.ExecuteReader();

            /* Store query results in a data table */
            DataTable results = new DataTable();
            results.Load(reader);

            if (results.Rows.Count == 0) return 0;

            if (results.Rows.Count > 1)
            {
                throw new Exception("More than 1 value returned for card name: " + p_cardName + " , set: " + p_set + ", foil: " + p_foil);
            }
            else
            {
                return results.Rows[0].Field<int>("card_id_int");
            }             
        }

        internal int GetNextKey(string p_nextKeyType)
        {
            ClearParameters();
            /* Build SQL query */
            Command.CommandText = "SELECT k.next_key_int FROM tblNextKeys k WHERE k.next_key_type_str = @p_nextKeyType";
            Command.Parameters.AddWithValue("@p_nextKeyType", p_nextKeyType);

            /* Execute query */
            SqlDataReader reader = Command.ExecuteReader();

            /* Store query results in a data table */
            DataTable results = new DataTable();
            results.Load(reader);

            /* Return next key */
            if (results.Rows.Count != 1)
            {
                throw new Exception("Unable to retrieve next key for type: " + p_nextKeyType);
            }
            else
            {
                return results.Rows[0].Field<int>("next_key_int");
            }
        }
        #endregion

        #region Inserts
        internal int InsertCardInfo(int p_cardId, string p_scryfallId, string p_cardName, string p_set, string p_type, string p_rarity, bool p_foil)
        {
            ClearParameters();
            Command.CommandText = "INSERT INTO tblCardInfo VALUES (@p_cardId, @p_scryfallId, @p_cardName, @p_set, @p_type, @p_rarity, @p_foil)";
            Command.Parameters.AddWithValue("@p_cardId", p_cardId);
            Command.Parameters.AddWithValue("@p_scryfallId", p_scryfallId);
            Command.Parameters.AddWithValue("@p_cardName", p_cardName);
            Command.Parameters.AddWithValue("@p_set", p_set);
            Command.Parameters.AddWithValue("@p_type", p_type);
            Command.Parameters.AddWithValue("@p_rarity", p_rarity);
            Command.Parameters.AddWithValue("@p_foil", p_foil);
            return Command.ExecuteNonQuery();
        }

        internal int InsertCardCount(int p_cardId, short p_count)
        {
            ClearParameters();
            Command.CommandText = "INSERT INTO tblCardCount VALUES (@p_cardId, @p_count)";
            Command.Parameters.AddWithValue("@p_cardId", p_cardId);
            Command.Parameters.AddWithValue("@p_count", p_count);
            return Command.ExecuteNonQuery();
        }

        internal int InsertCardPrice(int p_cardId, decimal p_price)
        {
            ClearParameters();
            Command.CommandText = "INSERT INTO tblCardPrice VALUES (@p_cardId, @p_price, @p_updated_date)";
            Command.Parameters.AddWithValue("@p_cardId", p_cardId);
            Command.Parameters.AddWithValue("@p_price", p_price);
            Command.Parameters.AddWithValue("@p_updated_date", DateTime.Now);
            return Command.ExecuteNonQuery();
        }

        internal int InsertCardColors(int p_cardId, short p_sequence, bool p_blue, bool p_black, bool p_red, bool p_green, bool p_white)
        {
            ClearParameters();
            Command.CommandText = "INSERT INTO tblCardColors VALUES (@p_cardId, @p_sequence, @p_blue, @p_black, @p_red, @p_green, @p_white)";
            Command.Parameters.AddWithValue("@p_cardId", p_cardId);
            Command.Parameters.AddWithValue("@p_sequence", p_sequence);
            Command.Parameters.AddWithValue("@p_blue", p_blue);
            Command.Parameters.AddWithValue("@p_black", p_black);
            Command.Parameters.AddWithValue("@p_red", p_red);
            Command.Parameters.AddWithValue("@p_green", p_green);
            Command.Parameters.AddWithValue("@p_white", p_white);
            return Command.ExecuteNonQuery();
        }

        internal int InsertCardManaCost(int p_cardId, short p_sequence, short p_uncolored, short p_blue, short p_black, short p_red, short p_green, short p_white, bool p_hybrid)
        {
            ClearParameters();
            Command.CommandText = "INSERT INTO tblCardManaCost VALUES (@p_cardId, @p_sequence, @p_uncolored, @p_blue, @p_black, @p_red, @p_green, @p_white, @p_hybrid)";
            Command.Parameters.AddWithValue("@p_cardId", p_cardId);
            Command.Parameters.AddWithValue("@p_sequence", p_sequence);
            Command.Parameters.AddWithValue("@p_uncolored", p_uncolored);
            Command.Parameters.AddWithValue("@p_blue", p_blue);
            Command.Parameters.AddWithValue("@p_black", p_black);
            Command.Parameters.AddWithValue("@p_red", p_red);
            Command.Parameters.AddWithValue("@p_green", p_green);
            Command.Parameters.AddWithValue("@p_white", p_white);
            Command.Parameters.AddWithValue("@p_hybrid", p_hybrid);
            return Command.ExecuteNonQuery();
        }
        #endregion

        #region Updates
        internal int UpdateCardCount(int p_cardId, short p_count)
        {
            ClearParameters();
            Command.CommandText = "UPDATE tblCardCount SET count_short = count_short + @p_count WHERE card_id_int = @p_cardId";
            Command.Parameters.AddWithValue("@p_cardId", p_cardId);
            Command.Parameters.AddWithValue("@p_count", p_count);
            return Command.ExecuteNonQuery();
        }

        internal int UpdateCardPrice(int p_cardId, decimal p_price)
        {
            ClearParameters();
            Command.CommandText = "UPDATE tblCardPrice SET price_dec = @p_price, updated_date = @p_updated_date WHERE card_id_int = @p_cardId";
            Command.Parameters.AddWithValue("@p_cardId", p_cardId);
            Command.Parameters.AddWithValue("@p_price", p_price);
            Command.Parameters.AddWithValue("@p_updated_date", DateTime.Now);
            return Command.ExecuteNonQuery();
        }

        internal int UpdateNextKey(string p_nextKeyType)
        {
            ClearParameters();
            Command.CommandText = "UPDATE tblNextKeys SET next_key_int = next_key_int + 1 WHERE next_key_type_str = @p_nextKeyType";
            Command.Parameters.AddWithValue("@p_nextKeyType", p_nextKeyType);
            return Command.ExecuteNonQuery();
        }
        #endregion

    }
}