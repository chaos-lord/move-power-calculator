using PokeApiNet;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq;
using System.Collections.Generic;
#nullable enable

namespace MovePowerCalculator
{
  class Program
  {
    static void Main(string[] args)
    {
        // instantiate client
        PokeApiClient pokeClient = new PokeApiClient();
        var mongoClient = new MongoClient();
        var database = mongoClient.GetDatabase("movePower");
        IMongoCollection<BsonDocument> pokeCollection = database.GetCollection<BsonDocument>("PokeSpecies");
        IMongoCollection<BsonDocument> evoCollection = database.GetCollection<BsonDocument>("PokeEvos");
        int numOfPokemon = 898; //does not include forms
        int numOfEvoChains = 475; // this includes single evos
        string[] formNames = new string[]{"deoxys-attack","deoxys-defend","deoxys-speed", "wormadam-sandy", "wormadam-trash", "shaymin-sky", "floette-eternal", "toxtricity-low-key", "indeedee-female",
         "kyurem-black", "kyurem-white", "meowstic-female", "hoopa-unbound","rattata-alola", "raticate-alola", "raichu-alola", "sandshrew-alola", "sandslash-alola", "vulpix-alola", "ninetales-alola",
         "diglett-alola", "dugtrio-alola", "meowth-alola", "persian-alola", "geodude-alola", "graveler-alola", "golem-alola", "grimer-alola", "muk-alola", "exeggutor-alola", "marowak-alola", "lycanroc-midnight",
         "wishiwashi-school", "lycanroc-dusk", "necrozma-dusk", "necrozma-dawn", "meowth-galar", "ponyta-galar", "rapidash-galar", "slowpoke-galar", "slowbro-galar", "farfetchd-galar", "weezing-galar", "mr-mime-galar",
         "articuno-galar", "zapdos-galar", "moltres-galar", "slowking-galar", "corsola-galar", "zigzagoon-galar", "linoone-galar", "darumaka-galar", "darmanitan-standard-galar", "yamask-galar", "stunfisk-galar",
         "indeedee-female", "urshifu-rapid-strike", "calyrex-ice-rider", "calyrex-shadow-rider"};

        for (int count = 1; count <= numOfEvoChains; count++)
        {
            EnterEvosToDB(pokeClient, count, evoCollection); 
        }

        for (int count = 1; count <= numOfPokemon; count++)
        {
            EnterPokesToDB(pokeClient, count, pokeCollection); 
        }

        foreach(string name in formNames)
        {
            EnterFormsToDB(pokeClient, name, pokeCollection); 
        }

    }

    static async void EnterPokesToDB(PokeApiClient pokeClient, int ID, IMongoCollection<BsonDocument> pokeCollection)
    {
            Pokemon currentPoke = await pokeClient.GetResourceAsync<Pokemon>(ID);
            var document = new BsonDocument{
                {"speciesName", currentPoke.Name},
                {currentPoke.Stats[0].Stat.Name, currentPoke.Stats[0].BaseStat},
                {currentPoke.Stats[1].Stat.Name, currentPoke.Stats[1].BaseStat},
                {currentPoke.Stats[2].Stat.Name, currentPoke.Stats[2].BaseStat},
                {currentPoke.Stats[3].Stat.Name, currentPoke.Stats[3].BaseStat},
                {currentPoke.Stats[4].Stat.Name, currentPoke.Stats[4].BaseStat},
                {currentPoke.Stats[5].Stat.Name, currentPoke.Stats[5].BaseStat},
                {"abilities", new BsonArray(currentPoke.Abilities.Select(A => A.Ability.Name).ToList())},
            };
            await pokeCollection.InsertOneAsync(document);
    }
        static async void EnterFormsToDB(PokeApiClient pokeClient, string name, IMongoCollection<BsonDocument> pokeCollection)
    {
            Pokemon currentPoke = await pokeClient.GetResourceAsync<Pokemon>(name);
            var document = new BsonDocument{
                {"speciesName", currentPoke.Name},
                {currentPoke.Stats[0].Stat.Name, currentPoke.Stats[0].BaseStat},
                {currentPoke.Stats[1].Stat.Name, currentPoke.Stats[1].BaseStat},
                {currentPoke.Stats[2].Stat.Name, currentPoke.Stats[2].BaseStat},
                {currentPoke.Stats[3].Stat.Name, currentPoke.Stats[3].BaseStat},
                {currentPoke.Stats[4].Stat.Name, currentPoke.Stats[4].BaseStat},
                {currentPoke.Stats[5].Stat.Name, currentPoke.Stats[5].BaseStat},
                {"abilities", new BsonArray(currentPoke.Abilities.Select(A => A.Ability.Name).ToList())},
            };
            await pokeCollection.InsertOneAsync(document);
    }
    static async void EnterEvosToDB(PokeApiClient pokeClient, int ID, IMongoCollection<BsonDocument> evoCollection)
    {
            EvolutionChain currentChain = await pokeClient.GetResourceAsync<EvolutionChain>(ID);
            ChainData evoMethods = ProcessEvoMethods(currentChain.Chain.EvolvesTo, currentChain.Chain.Species.Name);
            var document = new BsonDocument{
                {"chainId", currentChain.Id},
                {"base-form", currentChain.Chain.Species.Name},
                {"chain-details", evoMethods.Unfold()},
            };
            await evoCollection.InsertOneAsync(document);
    }

// the data structure is a list of mons that the base mon evolves from, each containing a string for the name, 
// the effective level they are evolved into and a list of thier own holding each mon they can evolve into's level and name
    static ChainData ProcessEvoMethods(List<ChainLink> nextEvos, string basicName) 
    {
        ChainData evoChain = new ChainData(basicName);
        foreach(ChainLink stageOneMon in nextEvos){
            string stageOneEvoName = stageOneMon.Species.Name;
            int? stageOneEvoLevel = GetEvoLevel(stageOneMon.EvolutionDetails[0]);
            List<ChainData> stageTwoEvos = new List<ChainData>();
            foreach(ChainLink stageTwoMon in stageOneMon.EvolvesTo){
                string stageTwoEvoName = stageTwoMon.Species.Name;
                int? stageTwoEvoLevel = GetEvoLevel(stageTwoMon.EvolutionDetails[0]);
                stageTwoEvos.Add(new ChainData(stageTwoEvoName, stageTwoEvoLevel));
            }
            if (evoChain.NextEvos == null){
                evoChain.NextEvos = new List<ChainData>();
            }
            evoChain.NextEvos.Add(new ChainData(stageOneEvoName, stageOneEvoLevel, stageTwoEvos));
        }
        return evoChain;
    }
    class ChainData{
        public string? Name;
        public int? Level;
        public List<ChainData>? NextEvos;
        public ChainData(string? _Name = null, int? _Level = null, List<ChainData>? _NextEvos = null){
            Level = _Level;
            Name = _Name;
            NextEvos = _NextEvos;
        }

        public (string?, int?, List<(string?, int?, List<(string?, int?)>?)>?) Unfold(){
            return (Name, Level, UnfoldStepTwo(NextEvos));
        }

        public List<(string?, int?, List<(string?, int?)>?)>? UnfoldStepTwo(List<ChainData>? stageOneEvos){
            List<(string?, int?, List<(string?, int?)>?)>? returnable = null;
            if (stageOneEvos != null){
                foreach (ChainData evolution in stageOneEvos){
                    if (returnable == null){
                        returnable = new List<(string?, int?, List<(string?, int?)>?)>();
                    }
                    returnable.Add((evolution.Name, evolution.Level, UnfoldStepThree(evolution.NextEvos)));
                }
            }
            return returnable;
        }
        public List<(string?, int?)>? UnfoldStepThree(List<ChainData>? stageTwoEvos){
            List<(string?, int?)>? returnable = null;
            if (stageTwoEvos != null){
                foreach (ChainData evolution in stageTwoEvos){
                    if (returnable == null){
                        returnable = new List<(string?, int?)>();
                    }
                    returnable.Add((evolution.Name, evolution.Level));
                }
            }
            return returnable;
        }
    }
    static int? GetEvoLevel(EvolutionDetail evo){
        
        switch (evo.Trigger.Name){
            case "level-up":
                if (evo.MinLevel != null){
                    return (int)evo.MinLevel;
                }
                //handle move evos and other triggers in species special cases
            break;
            case "shed":
                return 20;
        }
        return null;
  }
    static ChainData fixEvoChains(ChainData chainToFix){
        switch (chainToFix.Name){
            case "rotom":
                chainToFix.Name = "rotom-heat";
                break;
            case "wishiwashi":
                chainToFix.NextEvos = new List<ChainData>(){new ChainData("wishiwashi-school", 20)};
                break;
        }
        if(chainToFix.NextEvos != null){
            switch (chainToFix.NextEvos[0].Name){
                case "pikachu":
                    chainToFix.NextEvos.Add(new ChainData("raichu-alola"));
                    break;
                case "exeggcute":
                    chainToFix.NextEvos.Add(new ChainData("exeggutor-alola"));
                    break;
                case "cubone":
                    chainToFix.NextEvos.Add(new ChainData("marowak-alola", 28));
                    break;
                case "meowth":
                    chainToFix.NextEvos.RemoveAt(1);
                    break;
                case "farfetchd":
                    chainToFix.NextEvos = null;
                    break;
                case "koffing":
                    chainToFix.NextEvos.Add(new ChainData("weezing-galar", 35));
                    break;
                case "mr-mime":
                    chainToFix.NextEvos = null;
                    break;
                case "corsola":
                    chainToFix.NextEvos = null;
                    break;
                case "linoone":
                    chainToFix.NextEvos = null;
                    break;
                case "yamask":
                    chainToFix.NextEvos.RemoveAt(1);
                    break;
                case "burmy":
                    chainToFix.NextEvos.Add(new ChainData("wormadam-sandy", 20));
                    chainToFix.NextEvos.Add(new ChainData("wormadam-trash", 20));
                    break;
                case "rockruff":
                    chainToFix.NextEvos.Add(new ChainData("lycanroc-midnight", 25));
                    chainToFix.NextEvos.Add(new ChainData("lycanroc-dusk", 25));
                    break;
                case "toxel":
                    chainToFix.NextEvos.Add(new ChainData("toxtricity-low-key", 30));
                    break;
            }
        }
        return chainToFix;
    }

    static List<ChainData> addMissingEvoChains(){
        List<ChainData> extraChains = new List<ChainData>();
        extraChains.Add(new ChainData ("rattata-alola", null , new List<ChainData>(){new ChainData("raticate-alola", 20)}));
        extraChains.Add(new ChainData ("sandshrew-alola", null, new List<ChainData>(){new ChainData("sandslash-alola")}));
        extraChains.Add(new ChainData ("vulpix-alola", null, new List<ChainData>(){new ChainData("ninetales-alola")}));
        extraChains.Add(new ChainData ("diglett-alola", null, new List<ChainData>(){new ChainData("dugtrio-alola", 26)}));
        extraChains.Add(new ChainData ("meowth-alola", null, new List<ChainData>(){new ChainData("persian-alola")}));
        extraChains.Add(new ChainData ("geodude-alola", null, new List<ChainData>(){new ChainData("graveler-alola", 25, new List<ChainData>(){new ChainData("golem-alola")})}));
        extraChains.Add(new ChainData ("grimer-alola", null, new List<ChainData>(){new ChainData("muk-alola", 38)}));
        extraChains.Add(new ChainData ("meowth-galar", null, new List<ChainData>(){new ChainData("perrserker")}));
        extraChains.Add(new ChainData ("ponyta-galar", null, new List<ChainData>(){new ChainData("rapidash-galar", 40)}));
        extraChains.Add(new ChainData ("slowpoke-galar", 40, new List<ChainData>(){new ChainData("slowbro-galar"), new ChainData("slowking-galar")}));
        extraChains.Add(new ChainData ("farfetchd-galar", null, new List<ChainData>(){new ChainData("sirfetchd")}));
        extraChains.Add(new ChainData ("mime-jr", null, new List<ChainData>(){new ChainData("mr-mime-galar", null, new List<ChainData>(){new ChainData("mr-rime", 42)})}));
        extraChains.Add(new ChainData ("zapdos-galar"));
        extraChains.Add(new ChainData ("articuno-galar"));
        extraChains.Add(new ChainData ("moltres-galar"));
        extraChains.Add(new ChainData ("corsola-galar", null, new List<ChainData>(){new ChainData("cursola", 38)}));
        extraChains.Add(new ChainData ("zigzagoon-galar", null, new List<ChainData>(){new ChainData("linoone-galar", 20, new List<ChainData>(){new ChainData("obstagoon", 35)})}));
        extraChains.Add(new ChainData ("darumaka-galar", null, new List<ChainData>(){new ChainData("darmanitan-galar")}));
        extraChains.Add(new ChainData ("yamask-galar", null, new List<ChainData>(){new ChainData("runerigus")}));
        extraChains.Add(new ChainData ("stunfisk-galar"));
        extraChains.Add(new ChainData ("deoxys-attack"));
        extraChains.Add(new ChainData ("deoxys-speed"));
        extraChains.Add(new ChainData ("deoxys-defense"));
        extraChains.Add(new ChainData ("shaymin-sky"));
        extraChains.Add(new ChainData ("kyurem-white"));
        extraChains.Add(new ChainData ("kyurem-black"));
        extraChains.Add(new ChainData ("floette-eternal"));
        extraChains.Add(new ChainData ("hoopa-unbound"));
        extraChains.Add(new ChainData ("meowstic-female"));
        extraChains.Add(new ChainData ("indeedee-female"));
        extraChains.Add(new ChainData ("calyrex-ice-rider"));
        extraChains.Add(new ChainData ("calyrex-shadow-rider"));
        return extraChains;
    }

    static AbilityMultiplier CheckAbilityMultiplier(string ability){
        AbilityMultiplier multi = new AbilityMultiplier();
        switch (ability){
            case "adaptability":
                multi.STABMultipler = 1.333f;
                break;
            case "aerilate":
                multi.aerilateEnabled = true;
                break;
            case "analytic":
                multi.slowerMultiplier = 1.3f;
                break;
            case "battle-armor" or "shell-armor":
                multi.defenceMultipler = 1.02f;
                break;
            case "beast-boost":
                multi.physicalMultipler = 1.1f;
                multi.specialMultipler = 1.1f;
                multi.defenceMultipler = 1.1f;
                break;
            case "berserk":
                multi.berserkEnabled = true;
                break;
            case "blaze":
                multi.blazeEnabled = true;
                break;
            case "chilling-neigh":
                multi.physicalMultipler = 1.2f;
                break;
            case "compound-eyes":
                multi.accuracyMultiplier = 1.3f;
                break;
            case "contrary":
                multi.contraryEnabled = true;
                break;
            case "dark-aura":
                multi.darkAuraEnabled = true;
                break;
            case "dauntless-shield" or "intimidate" or "stamina":
                multi.defenceMultipler = 1.25f;
                break;
            case "defeatist":
                multi.defeatistEnabled = true;
                break;
            case "desolate-land" or "drought":
                multi.sunEnabled = true;
                break;
            case "disguise":
                multi.disguiseEnabled = true;
                break;
            case "download" or "parental-bond":
                multi.physicalMultipler = 1.25f;
                multi.specialMultipler = 1.25f;
                break;
            case "dragon-energy":
                multi.STABMultipler = 1.5f;
                break;
            case "drizzle" or "primordial-sea":
                multi.rainEnabled = true;
                break;
            case "electric-surge":
                multi.electricSurgeEnabled = true;
                break;
            case "fairy-aura":
                multi.fairyAuraEnabled = true;
                break;
            case "fluffy" or "fur-coat" or "ice-scales":
                multi.defenceMultipler = 1.5f;
                break;
            case "galvanize":
                multi.galvanizeEnabled = true;
                break;
            case "gorilla-tactics":
                multi.physicalMultipler = 1.4f;
                break;
            case "grassy-surge":
                multi.grassySurgeEnabled = true;
                break;
            case "grim-neigh" or "soul-heart":
                multi.specialMultipler = 1.2f;
                break;
            case "huge-power" or "pure-power":
                multi.physicalMultipler = 2f;
                break;
            case "ice-face":
                multi.iceFaceEnabled = true;
                break;
            case "intrepid-sword":
                multi.physicalMultipler = 1.5f;
                break;
            case "iron-fist":
                multi.ironFistEnabled = true;
                break;
            case "libero" or "protean":
                multi.liberoEnabled = true;
                break;
            case "multiscale" or "shadow-shield":
                multi.multiscaleEnabled = true;
                break;
            case "no-guard":
                multi.noGuardEnabled = true;
                break;
            case "overgrow":
                multi.overgrowEnabled = true;
                break;
            case "pixilate":
                multi.pixilateEnabled = true;
                break;
            case "psychic-surge":
                multi.psychicSurgeEnabled = true;
                break;
            case "punk-rock":
                multi.punkRockEnabled = true;
                break;
            case "reckless":
                multi.recklessEnabled = true;
                break;
            case "refrigirate":
                multi.refrigirateEnabled = true;
                break;
            case "serene-grace":
                multi.sereneGraceEnabled = true;
                break;
            case "sheer-force":
                multi.sheerForceEnabled = true;
                break;
            case "shields-down":
                multi.shieldsDownEnabled = true;
                break;
            case "skill-link":
                multi.skillLinkEnabled = true;
                break;
            case "slow-start":
                multi.slowStartEnabled = true;
                break;
            case "sniper":
                multi.sniperEnabled = true;
                break;
            case "snow-warning":
                multi.snowWarningEnabled = true;
                break;
            case "speed-boost":
                multi.speedBoostEnabled = true;
                break;
            case "steelworker" or "steely-spirit":
                multi.steelworkerEnabled = true;
                break;
            case "stench":
                multi.fasterMultiplier = 1.1f;
                break;
            case "strong-jaw":
                multi.strongJawEnabled = true;
                break;
            case "sturdy":
                multi.sturdyEnabled = true;
                break;
            case "super-luck":
                multi.superLuckEnabled = true;
                break;
            case "swarm":
                multi.swarmEnabled = true;
                break;
            case "technitian":
                multi.technitianEnabled = true;
                break;
            case "torrent":
                multi.torrentEnabled = true;
                break;
            case "touge-claws":
                multi.toughClawsEnabled = true;
                break;
            case "transistor":
                multi.transistorEnabled = true;
                break;
            case "truant":
                multi.truantEnabled = true;
                break;
            case "victory-star":
                multi.accuracyMultiplier = 1.1f;
                break;
            case "water-bubble":
                multi.waterBubbleEnabled = true;
                break;
        }
        return multi;
    }

    class AbilityMultiplier{
        public float physicalMultipler, specialMultipler, defenceMultipler, STABMultipler, slowerMultiplier, fasterMultiplier, accuracyMultiplier = 1;
        public bool aerilateEnabled = false;
        public bool berserkEnabled = false;
        public bool blazeEnabled = false;
        public bool contraryEnabled = false;
        public bool darkAuraEnabled = false;
        public bool defeatistEnabled = false;
        public bool sunEnabled = false;
        public bool disguiseEnabled = false;
        public bool rainEnabled = false;
        public bool electricSurgeEnabled = false;
        public bool fairyAuraEnabled = false;
        public bool galvanizeEnabled = false;
        public bool grassySurgeEnabled = false;
        public bool iceFaceEnabled = false;
        public bool ironFistEnabled = false;
        public bool liberoEnabled = false;
        public bool multiscaleEnabled = false;
        public bool noGuardEnabled = false;
        public bool overgrowEnabled = false;
        public bool pixilateEnabled = false;
        public bool psychicSurgeEnabled = false;
        public bool punkRockEnabled = false;
        public bool recklessEnabled = false;
        public bool refrigirateEnabled = false;
        public bool sereneGraceEnabled = false;
        public bool sheerForceEnabled = false;
        public bool shieldsDownEnabled = false;
        public bool skillLinkEnabled = false;
        public bool slowStartEnabled = false;
        public bool sniperEnabled = false;
        public bool snowWarningEnabled = false;
        public bool speedBoostEnabled = false;
        public bool steelworkerEnabled = false;
        public bool strongJawEnabled = false;
        public bool sturdyEnabled = false;
        public bool superLuckEnabled = false;
        public bool swarmEnabled = false;
        public bool technitianEnabled = false;
        public bool torrentEnabled = false;
        public bool toughClawsEnabled = false;
        public bool transistorEnabled = false;
        public bool truantEnabled = false;
        public bool waterBubbleEnabled = false;
        }
    
    }
  
}


