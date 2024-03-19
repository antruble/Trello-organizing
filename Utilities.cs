﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trello.Models;

namespace Trello
{
    public class Utilities
    {
        /**
         * A paraméterben megkapott hónapban, a kapott list-nek megfelelő SHOP-ban, 
         * a kapott labelId alapján frissíti a számlálót.
         * **/
        public static void UpdateShopCounters(Completed date, string list, string cardName, int weight, string operation = "ADD")
        {
            // SHOP LEKÉRDEZÉSE TRELLO LISTA ID ALAPJÁN
            string? shop = getShopByListId(list);

            // HA A TRELLO LISTA ID ORDERSHEZ TARTOZIK -> TÖBB SHOP TASKJAI SZEREPELNEK BENNE ->
            // --> KÁRTYA NEVÉBEN SZEREPLŐ AZONOSÍTÓ ALAPJÁN KELL LEKÉRDEZNI A SHOPOT
            if (shop == "ORDERS")
                shop = getShopByCardName(cardName);

            // FONTOSSÁGI SÚLYOK LEKÉRÉSE
            var labels = Settings.GetLabels();

            // CALCHELPER === 1     -> KÁRTYA EL LETT FOGADVA   -> SZÁMLÁLÓT NÖVELNI KELL ||
            // CALCHELPER === -1    -> KÁRTYA ÚJRA LETT NYITVA  -> SZÁMLÁLÓT CSÖKKENTENI KELL
            int calcHelper = 1;
            // HA NEM "ADD" AZ OPERÁTOR, AKKOR KIVONNI KELL
            if (operation != "ADD")
                calcHelper = -1;

            // Csak akkor frissíthető az adat, ha valamelyik shop listájához tartozik
            if (shop != null)
            {
                // ÖSSZESÍTETT SZÁMLÁLÓ NÖVELÉSE
                date.AllCompleted += calcHelper;

                // A MEGFELELŐ SHOP MEGFELELŐ SÚLYOZÁSÁNAK FRISSÍTÉSE
                switch (shop)
                {
                    case "SHOPERIA":
                        date.ShoperiaAllCompleted+= calcHelper;
                        switch (weight) 
                        {
                            case 0:
                                date.Shoperia_UnWeighted += calcHelper;
                                break;
                            case 1:
                                date.Shoperia_W1 += calcHelper;
                                break;
                            case 2:
                                date.Shoperia_W2 += calcHelper;
                                break;
                            case 3:
                                date.Shoperia_W3 += calcHelper;
                                break;
                        }
                        return;
                    case "HOME12":
                        date.Home12AllCompleted += calcHelper;
                        switch (weight)
                        {
                            case 0:
                                date.Home12_UnWeighted += calcHelper;
                                break;
                            case 1:
                                date.Home12_W1 += calcHelper;
                                break;
                            case 2:
                                date.Home12_W2 += calcHelper;
                                break;
                            case 3:
                                date.Home12_W3 += calcHelper;
                                break;
                        }
                        return;
                    case "MATEBIKE":
                        date.MatebikeAllCompleted+= calcHelper;
                        switch (weight)
                        {
                            case 0:
                                date.Matebike_UnWeighted+= calcHelper;
                                break;
                            case 1:
                                date.Matebike_W1 += calcHelper;
                                break;
                            case 2:
                                date.Matebike_W2 += calcHelper;
                                break;
                            case 3:
                                date.Matebike_W3 += calcHelper;
                                break;
                        }
                        return;
                    case "XPRESS":
                        date.XpressAllCompleted += calcHelper;
                        switch (weight)
                        {
                            case 0:
                                date.Xpress_UnWeighted += calcHelper;
                                break;
                            case 1:
                                date.Xpress_W1 += calcHelper;
                                break;
                            case 2:
                                date.Xpress_W2 += calcHelper;
                                break;
                            case 3:
                                date.Xpress_W3 += calcHelper;
                                break;
                        }
                        return;
                }
            }
        }

        /**
         * Kap egy lista Id-t paraméterül, és megkeresi, hogy az melyik shophoz tartozik, ha egyikhez se, akkor nullal tér vissza
         * **/
        static string? getShopByListId(string list)
        {
            var listIDs = Settings.GetLists();
            if (listIDs.Shoperia != null && listIDs.Home12 != null && listIDs.Xpress != null && listIDs.Matebike != null)
            {
                foreach (string listID in listIDs.Shoperia)
                    if (listID == list)
                        return "SHOPERIA";
                foreach (string listID in listIDs.Home12)
                    if (listID == list)
                        return "HOME12";
                foreach (string listID in listIDs.Xpress)
                    if (listID == list)
                        return "XPRESS";
                foreach (string listID in listIDs.Matebike)
                    if (listID == list)
                        return "MATEBIKE";
                if (list == listIDs.Orders)
                    return "ORDERS";
            }
            return null;
        }
        static string? getShopByCardName(string cardName) 
        { 
            string shopId = cardName.Split('-')[0];
            switch (shopId) 
            {
                case "SH":
                    return "SHOPERIA";
                case "XP":
                    return "XPRESS";
                case "HOME12":
                    return "HOME12";
                case "MATE":
                    return "MATEBIKE";
            }
            return null;
        }

        public static int GetWeightFromLabels(List<string> labels) 
        {
            var weightedLabels = Settings.GetLabels();
            int tempMax = 0;
            foreach (string label in labels) 
            {
                if (label == weightedLabels.Weight3)
                    return 3;
                else if (label == weightedLabels.Weight2)
                    tempMax = 2;
                else if (tempMax < 2 && label == weightedLabels.Weight1)
                    tempMax = 1;
            }
            return tempMax;

        }
        public static void UpdateWeight(int oldWeight, int newWeight) 
        { 
            
        }
    }
}
