using System.Data;
using System.Data.SqlClient;

namespace MagicCardInventory
{
    internal class SQL
    {
        #region Properties
        private SqlConnection Con { get; set; }
        private SqlTransaction? Transaction { get; set; }
        private bool Rollback { get; set; }
        #endregion

        #region Constructors
        public SQL()
        {
            Con = new SqlConnection();
            Rollback = false;
        }

        public SQL(string connectionString)
        {
            Con = new SqlConnection(connectionString);
            Rollback = false;
        }
        #endregion

        #region Transactions
        internal void BeginTransaction()
        {
            Con.Open();
            Transaction = Con.BeginTransaction();
        }

        internal bool CommitTransaction()
        {
            if (Transaction is null)
            {
                throw new Exception("Cannot commit without an open transaction");
            }

            /* Commit transaction */
            try
            {
                Transaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error committing transaction: " + ex.GetType() + ", Message: " + ex.Message);
                Rollback = true;
                return false;
            }

            Con.Close();
            return true;
        }

        internal void RollbackTransaction()
        {
            if (Transaction is null)
            {
                throw new Exception("Cannot rollback without an open transaction");
            }

            if (!Rollback)
            {
                throw new Exception("Cannot rollback a transaction that was not attempted to be committed");
            }

            /* Rollback transaction */
            try
            {
                Transaction.Rollback();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error rolling back transaction: " + ex.GetType() + ", Message: " + ex.Message);
            }

            Rollback = false;
            Con.Close();
        }
        #endregion

        #region Selects
        internal DataTable GetAllCards()
        {
            string sql = "SELECT c.card_id_int, c.scryfall_id_str, c.foil_bool FROM tblCardInfo c";
            SqlCommand command = new(sql, Con, Transaction);
            SqlDataReader reader = command.ExecuteReader();
            DataTable results = new();
            results.Load(reader);
            return results;
        }
        internal int GetCardId(string p_cardName, string p_set, bool p_foil)
        {
            /* Build SQL query */
            string sql = "SELECT c.card_id_int FROM tblCardInfo c WHERE c.card_name_str = @p_cardName AND c.set_str = @p_set AND c.foil_bool = @p_foil";
            SqlCommand command = new(sql, Con, Transaction);
            command.Parameters.AddWithValue("@p_cardName", p_cardName);
            command.Parameters.AddWithValue("@p_set", p_set);
            command.Parameters.AddWithValue("@p_foil", p_foil);

            /* Execute query */
            SqlDataReader reader = command.ExecuteReader();

            /* Store query results in a data table */
            DataTable results = new();
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
            /* Build SQL query */
            string sql = "SELECT k.next_key_int FROM tblNextKeys k WHERE k.next_key_type_str = @p_nextKeyType";
            SqlCommand command = new(sql, Con, Transaction);
            command.Parameters.AddWithValue("@p_nextKeyType", p_nextKeyType);

            /* Execute query */
            SqlDataReader reader = command.ExecuteReader();

            /* Store query results in a data table */
            DataTable results = new();
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
        internal int InsertCardInfo(int p_cardId, string p_scryfallId, string p_cardName, string p_set, string p_rarity, bool p_foil)
        {
            string sql = "INSERT INTO tblCardInfo VALUES (@p_cardId, @p_scryfallId, @p_cardName, @p_set, @p_rarity, @p_foil)";
            SqlCommand command = new(sql, Con, Transaction);
            command.Parameters.AddWithValue("@p_cardId", p_cardId);
            command.Parameters.AddWithValue("@p_scryfallId", p_scryfallId);
            command.Parameters.AddWithValue("@p_cardName", p_cardName);
            command.Parameters.AddWithValue("@p_set", p_set);
            command.Parameters.AddWithValue("@p_rarity", p_rarity);
            command.Parameters.AddWithValue("@p_foil", p_foil);
            return command.ExecuteNonQuery();
        }

        internal int InsertCardCount(int p_cardId)
        {
            string sql = "INSERT INTO tblCardCount VALUES (@p_cardId, 1)";
            SqlCommand command = new(sql, Con, Transaction);
            command.Parameters.AddWithValue("@p_cardId", p_cardId);
            return command.ExecuteNonQuery();
        }

        internal int InsertCardPrice(int p_cardId, decimal p_price)
        {
            string sql = "INSERT INTO tblCardPrice VALUES (@p_cardId, @p_price, @p_updated_date)";
            SqlCommand command = new(sql, Con, Transaction);
            command.Parameters.AddWithValue("@p_cardId", p_cardId);
            command.Parameters.AddWithValue("@p_price", p_price);
            command.Parameters.AddWithValue("@p_updated_date", DateTime.Now);
            return command.ExecuteNonQuery();
        }
        #endregion

        #region Updates
        internal int UpdateCardCount(int p_cardId)
        {
            string sql = "UPDATE tblCardCount SET count_short = count_short + 1 WHERE card_id_int = @p_cardId";
            SqlCommand command = new(sql, Con, Transaction);
            command.Parameters.AddWithValue("@p_cardId", p_cardId);
            return command.ExecuteNonQuery();
        }

        internal int UpdateCardPrice(int p_cardId, decimal p_price)
        {
            string sql = "UPDATE tblCardPrice SET price_dec = @p_price, updated_date = @p_updated_date WHERE card_id_int = @p_cardId";
            SqlCommand command = new(sql, Con, Transaction);
            command.Parameters.AddWithValue("@p_cardId", p_cardId);
            command.Parameters.AddWithValue("@p_price", p_price);
            command.Parameters.AddWithValue("@p_updated_date", DateTime.Now);
            return command.ExecuteNonQuery();
        }

        internal int UpdateNextKey(string p_nextKeyType)
        {
            string sql = "UPDATE tblNextKeys SET next_key_int = next_key_int + 1 WHERE next_key_type_str = @p_nextKeyType";
            SqlCommand command = new(sql, Con, Transaction);
            command.Parameters.AddWithValue("@p_nextKeyType", p_nextKeyType);
            return command.ExecuteNonQuery();
        }
        #endregion

    }
}