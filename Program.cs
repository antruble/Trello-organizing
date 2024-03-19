using Manatee.Trello;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq.Expressions;
using Trello;
using Trello.Models;

var settings = Settings.LoadSettings();


if (settings.BoardId == null || settings.ConnectionString == null)
{
    throw new Exception("settings can't be null");
}
TrelloAuthorization.Default.AppKey = settings.ApiKey;
TrelloAuthorization.Default.UserToken = settings.UserSecret;

// LISTÁK ELLENŐRZÉSE
await CheckLists(settings.BoardId, settings.ConnectionString);

static async Task CheckLists(string boardId, string connectionstring)
{
    // <trello beállítása>
    ITrelloFactory factory = new TrelloFactory();
    var board = factory.Board(boardId);
    await board.Lists.Refresh();
    var trelloLists = board.Lists;
    // </trello beállítása>

    // SQL KAPCSOLAT LÉTREHOZÁSA
    var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionstring).Options;
    using (var dbContext = new ApplicationDbContext(dbOptions))
    {
        try
        {
            // ADATBÁZIS TÁBLÁK ELLENŐRZÉSE, HA VALAMELYIK HIÁNYZIK -> LÉTREHOZÁS
            CheckDatabaseTables(dbContext);

            // ADATBÁZISBAN LÉTEZŐ LISTÁK KIGYŰJTÉSE A KÁRTYÁIKKAL
            var dbLists = dbContext.Lists?.Include(a => a.Cards).ToList();

            // EGYESÉVEL MINDEN TRELLO LISTA ELLENŐRZÉSE
            foreach (var trelloList in trelloLists)
            {
                await trelloList.Refresh();
                // ADOT TRELLO LISTA MEGKERESÉSE A DB-BEN
                var dbList = dbLists?.Find(l => l.Id?.Trim() == trelloList.Id.Trim());
                // NEM SZEREPEL A DB-BEN -> LÉTRE KELL HOZNI
                if (dbList == null)
                {
                    dbList = new ListModel
                    {
                        Name = trelloList.Name,
                        Id = trelloList.Id,
                        Cards = new List<CardModel>(),
                    };
                    dbContext.Lists?.Add(dbList);
                    dbContext.SaveChanges();
                }

                var trelloCards = trelloList.Cards;
                // A LISTÁBAN SZEREPLŐ KÁRTYÁK VIZSGÁLATA EGYESÉVEL
                foreach (var trelloCard in trelloCards)
                {
                    // HA ARCHIVÁLVA VAN NEM VIZSGÁLJUK
                    if (trelloCard.IsArchived == true)
                        break;

                    // ADOTT TRELLO KÁRTYA MEGKERESÉSE DB-BEN
                    var dbCard = dbList.Cards?.FirstOrDefault(c => c.Id?.Trim() == trelloCard.Id.Trim());
                    // NEM LÉTEZIK A KÁRTYA AZ ADATBÁZISBAN -> HOZZÁ KELL ADNI
                    if (dbCard == null)
                    {
                        int weight;
                        if (trelloCard.Labels == null)
                            weight = 0;
                        else
                        {
                            List<string> labelIDs = new List<string>();
                            foreach (var label in trelloCard.Labels)
                                labelIDs.Add(label.Id);

                            weight = Utilities.GetWeightFromLabels(labelIDs);
                        };
                        var newCard = new CardModel
                        {
                            Id = trelloCard.Id,
                            Date = (DateTime) trelloCard.LastActivity,
                            Name = trelloCard.Name,
                            Weight = weight,
                            IsComplete = trelloCard.IsComplete,
                            List = dbList,
                            ListId = trelloList.Id,
                        };
                        // A KÁRTYA MÁR EL LETT FOGADVA MIELŐTT BEKERÜLT VOLNA A DB-BE -> DOKUMENTÁLNI KELL
                        if (trelloCard.IsComplete == true)
                        {
                            // A KÁRTYA ELFOGADÁSÁNAK ÉVÉNEK ÉS HÓNAPJÁNAK KIGYŰJTÉSE
                            int year = (trelloCard.LastActivity ?? throw new Exception()).Year;
                            int month = (trelloCard.LastActivity ?? throw new Exception()).Month;
                            
                            // KÁRTYA DÁTUMA MEGKERESÉSE DB-BEN -> A VISSZAKAPOTT SORT KELL FRISSÍTENI
                            var dbRowToUpdate = dbContext.Completed?.FirstOrDefault(e => e.Date == new DateTime(year, month, 1));
                            
                            // HA ILYEN DÁTUM NEM SZEREPEL A DB-BEN -> LÉTRE KELL HOZNI
                            if (dbRowToUpdate == null)
                            {
                                dbContext.addDateToDB(year, month);
                                dbRowToUpdate = dbContext.Completed?.FirstOrDefault(e => e.Date == new DateTime(year, month, 1));
                                if (dbRowToUpdate == null)
                                    throw new Exception("Error while creating new date: searched row is still null");
                            }

                            if (trelloCard.Labels != null)
                            {
                                // DB-BEN A SZÁMLÁLÓ FRISSÍTÉSE
                                Utilities.UpdateShopCounters(dbRowToUpdate, trelloList.Id, trelloCard.Name, weight);
                            }
                        }
                        dbList.Cards?.Add(newCard);
                        dbContext.SaveChanges();
                    }
                    // LÉTEZIK A KÁRTYA 
                    else 
                    {
                        // ALAP ADATOK LEKÉRÉSE -> KÁRTYA A DB-BEN, ILLETVE A TRELLO KÁRTYA SÚLYA
                        // A KÁRTYA ELFOGADÁSÁNAK ÉVÉNEK ÉS HÓNAPJÁNAK KIGYŰJTÉSE
                        int year = (trelloCard.LastActivity ?? throw new Exception()).Year;
                        int month = (trelloCard.LastActivity ?? throw new Exception()).Month;

                        // KÁRTYA DÁTUMA MEGKERESÉSE DB-BEN -> A VISSZAKAPOTT SORT KELL FRISSÍTENI
                        var dbRowToUpdate = dbContext.Completed?.FirstOrDefault(e => e.Date == new DateTime(year, month, 1));
                        
                        // HA ILYEN DÁTUM NEM SZEREPEL A DB-BEN -> LÉTRE KELL HOZNI
                        if (dbRowToUpdate == null)
                        {
                            dbContext.addDateToDB(year, month);
                            dbRowToUpdate = dbContext.Completed?.FirstOrDefault(e => e.Date == new DateTime(year, month, 1));
                            if (dbRowToUpdate == null)
                                throw new Exception("Error while creating new date: searched row is still null");
                        }
                        // A KÁRTYÁN LÉVŐ LABELEK KÖZÜL KIVÁLASZTJUK A LEGNAGYOBB FONTOSSÁGI SÚLYÚT, HA NINCS ->
                        // -> weight = 0
                        int weight;
                        if (trelloCard.Labels == null)
                            weight = 0;
                        else
                        {
                            List<string> labelIDs = new List<string>();
                            foreach (var label in trelloCard.Labels)
                                labelIDs.Add(label.Id);
                            weight = Utilities.GetWeightFromLabels(labelIDs);
                        }

                        // [(A) TRELLOBAN EL LETT FOGADVA || (B) TRELLOBAN ÚJRA LETT NYITVA] => DOKUMENTÁLNI KELL
                        if (trelloCard.IsComplete != dbCard.IsComplete)
                        {
                            dbCard.IsComplete = trelloCard.IsComplete;
                            // (A) EL LETT FOGADVA
                            if (trelloCard.IsComplete == true)
                                Utilities.UpdateShopCounters(dbRowToUpdate, trelloList.Id, trelloCard.Name, weight);
                            // (B) ÚJRA LETT NYITVA
                            else
                                Utilities.UpdateShopCounters(dbRowToUpdate, trelloList.Id, trelloCard.Name, weight, "REMOVE");

                            dbContext.SaveChanges();
                        }
                        
                        // VÁLTOZOTT SÚLYOZÁS
                        if (weight != dbCard.Weight) 
                        {
                            if (trelloCard.IsComplete == true)
                            {
                                if (dbCard.IsComplete == true)
                                {
                                    dbRowToUpdate = dbContext.Completed?.FirstOrDefault(e => e.Date == new DateTime(dbCard.Date.Year, dbCard.Date.Month, 1));

                                    // HA ILYEN DÁTUM NEM SZEREPEL A DB-BEN -> LÉTRE KELL HOZNI
                                    if (dbRowToUpdate == null)
                                    {
                                        dbContext.addDateToDB(year, month);
                                        dbRowToUpdate = dbContext.Completed?.FirstOrDefault(e => e.Date == new DateTime(year, month, 1));
                                        if (dbRowToUpdate == null)
                                            throw new Exception("Error while creating new date: searched row is still null");
                                    }
                                }
                                Utilities.UpdateShopCounters(dbRowToUpdate, trelloList.Id, trelloCard.Name, dbCard.Weight, "REMOVE");
                                Utilities.UpdateShopCounters(dbRowToUpdate, trelloList.Id, trelloCard.Name, weight);
                            }
                            dbCard.Weight = weight;
                            dbContext.SaveChanges();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database save error: " + ex.Message);
        }
    }
}

static void CheckDatabaseTables(ApplicationDbContext dbContext) 
{
    try 
    {
        var connection = dbContext.Database.GetDbConnection();
        connection.Open();
        var command = connection.CreateCommand();

        // TÁBLÁS LEKÉRDEZÉSE, HA NEM LÉTEZIK FALSE-SZAL TÉR VISSZA
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Lists'";
        bool isListsTableExist = (Convert.ToInt32(command.ExecuteScalar()) <= 0);
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Cards'";
        bool isCardsTableExist = (Convert.ToInt32(command.ExecuteScalar()) <= 0);
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Completed'";
        bool isCompletedTableExist = (Convert.ToInt32(command.ExecuteScalar()) <= 0);
        connection.Close();

        // NEM LÉTEZIK LISTS TÁBLA AZ ADATBÁZISBAN -> LÉTRE KELL HOZNI
        if (isListsTableExist)
            dbContext.createTable("Lists");
        // NEM LÉTEZIK CARDS TÁBLA AZ ADATBÁZISBAN -> LÉTRE KELL HOZNI
        if (isCardsTableExist)
            dbContext.createTable("Cards");
        // NEM LÉTEZIK COMPLETED TÁBLA AZ ADATBÁZISBAN -> LÉTRE KELL HOZNI
        if (isCompletedTableExist)
            dbContext.createTable("Completed");
    } 
    catch (Exception ex)
    {
        Console.WriteLine("Database table create error: " + ex.Message);
    }

    
}



