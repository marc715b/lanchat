using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Data;

namespace console_test.Logic
{

    public class Contact
    {
        private static readonly int MAX_NAME_LEN = 32;
        private static readonly int ECDH_PUBKEY_LEN = 65; // NOTE: ECDH pub key represented as hex = 65 letters
                                                          // TODO: move into Crypto.cs file later

        private string _name;
        private string _ip; // not needed?
        private string _pubKey;
        private string _sharedKey;

        private static readonly List<Contact> _contacts = new List<Contact>();

        public Contact(string name, string ip, string pubKey)
        {
            // Set all fields with input verification
            SetName(name);
            SetIp(ip); // not needed?
            SetPubKey(pubKey);
            SetSharedKey("1"); // just so db method doesnt try to insert a null val

            AddToContactList(this);
        }
        public Contact(string name, string ip, string pubKey, string sharedKey)
        {
            // Set all fields with input verification
            SetName(name);
            SetIp(ip); // not needed?
            SetPubKey(pubKey);
            SetSharedKey(sharedKey);

            AddToContactList(this);
        }
        public static List<Contact> GetAllContacts()
        {
            return _contacts;
        }

        public static void AddToContactList(Contact contact)
        {
                // Add new contact
                _contacts.Add(contact);
        }
        public void SetName(string name)
        {
            if (name.Length > MAX_NAME_LEN)
                throw new ArgumentException("Contact display name is too long");

            _name = name;
        }

        public string GetName()
        {
            return _name;
        }

        public void SetIp(string ip)
        {
            IPAddress tempAddress;
            if (!IPAddress.TryParse(ip, out tempAddress))
                throw new ArgumentException("IP address is invalid");

            _ip = ip;
        }

        public string GetIp()
        {
            return _ip;
        }

        public void SetPubKey(string pubKey)
        {
            if (pubKey.Length != ECDH_PUBKEY_LEN)
                throw new ArgumentException("Invalid public key length");

            _pubKey = pubKey;
        }

        public string GetPubKey()
        {
            return _pubKey;
        }

        public void SetSharedKey(string sharedKey)
        {
         //   if (sharedKey.Length != whatever length) 
           //     throw new ArgumentException("Invalid shared key length");
            _sharedKey = sharedKey;
        }

        public string GetSharedKey()
        {
            return _sharedKey;
        }
    }


    public class SqliteContactManager : IDisposable
    {
        private SqliteConnection _connection;
        private readonly string _dbPath;

        public SqliteContactManager(string dbPath)
        {
            _dbPath = dbPath;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool createTable = !File.Exists(_dbPath);

            _connection = new SqliteConnection($"Data Source={_dbPath};");
            _connection.Open();

            if (createTable)
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                    CREATE TABLE Contacts (
                        Name TEXT PRIMARY KEY,
                        IP TEXT NOT NULL,
                        PubKey TEXT NOT NULL,
                        SharedKey TEXT NOT NULL
                    );";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SyncContacts(List<Contact> contacts)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    foreach (var contact in contacts)
                    {
                        using (var command = _connection.CreateCommand())
                        {
                            // Check if contact exists
                            command.CommandText = "SELECT COUNT(*) FROM Contacts WHERE Name = @Name";
                            command.Parameters.AddWithValue("@Name", contact.GetName());
                            int count = Convert.ToInt32(command.ExecuteScalar());

                            if (count > 0)
                            {
                                // Update existing contact
                                command.CommandText = @"
                                UPDATE Contacts 
                                SET IP = @IP, PubKey = @PubKey, SharedKey = @SharedKey
                                WHERE Name = @Name";
                            }
                            else
                            {
                                // Insert new contact
                                command.CommandText = @"
                                INSERT INTO Contacts (Name, IP, PubKey, SharedKey)
                                VALUES (@Name, @IP, @PubKey, @SharedKey)";
                            }

                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@Name", contact.GetName());
                            command.Parameters.AddWithValue("@IP", contact.GetIp());
                            command.Parameters.AddWithValue("@PubKey", contact.GetPubKey());
                            command.Parameters.AddWithValue("@SharedKey", contact.GetSharedKey());

                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        public List<Contact> GetDBcontacts()
        {
            List<Contact> contacts = new List<Contact>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT Name, IP, PubKey, SharedKey FROM Contacts";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Contact contact = new Contact(
                            reader["Name"].ToString(),
                            reader["IP"].ToString(),
                            reader["PubKey"].ToString(),
                            reader["SharedKey"].ToString()
                        );
                        contacts.Add(contact);
                    }
                }
            }
            return contacts;
        }
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
        }
    }
}
