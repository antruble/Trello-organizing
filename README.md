# Trello-organizing

Ez a kód arra szolgál, hogy megszámolja a Trello-ban lévő már elvégzett és elfogadott feladatokat, a hozzájuk rendelt cégek alapján és feladat fontossági súlyozása alapján.

Használat
A kód használata előtt ki kell tölteni az appsettings.json fájlt az adatbázis connection strinjével, a trello user secrettel, az API kulccsal és a táblázat azonosítójával.

json
{
  "settings": {
    "connectionString": "CONNECTION_STRING_HELYE",
    "userSecret": "USER_SECRET_HELYE",
    "apiKey": "API_KEY_HELYE",
    "boardId": "TRELLO_BOARD_ID_HELYE"
  }
}
A kód működése során összehasonlítja az újonnan lekért adatokat a saját adatbázisával. Három lehetőség van:

Ha a vizsgált kártya nem létezik az adatbázisban, létrehozza azt.
Ha a kártya létezik az adatbázisban, de különbözik, frissíti az újonnan lekért adatokkal.
Ha egy kártya prioritása megváltozik, és a kártyát korábban is elfogadták, és most is elfogadják, frissíti az adatbázisban a legutóbbi módosított hónapban.
