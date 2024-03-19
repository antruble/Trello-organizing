# Trello-organizing
# Trello Feladat Számláló
# Ez a kód arra szolgál, hogy megszámolja a Trello-ban lévő már elvégzett és elfogadott feladatokat, a hozzájuk rendelt vállalatok alapján és azok prioritása szerint.

Használat
A kód használata előtt ki kell tölteni az appsettings.json fájlt a felhasználói titokkal, az API kulccsal és a táblázat azonosítójával.

json
Copy code
{
  "TrelloSettings": {
    "UserSecret": "a_felhasznaloi_titokod",
    "ApiKey": "a_api_kulcsod",
    "BoardId": "a_tablazat_azonositoja"
  }
}
A kód működése során összehasonlítja az újonnan lekért adatokat a saját adatbázisával. Három lehetőség van:

Ha a vizsgált kártya nem létezik az adatbázisban, létrehozza azt.
Ha a kártya létezik az adatbázisban, de különbözik, frissíti az újonnan lekért adatokkal.
Ha egy kártya prioritása megváltozik, és a kártyát korábban is elfogadták, és most is elfogadják, frissíti az adatbázisban a legutóbbi módosított hónapban.
