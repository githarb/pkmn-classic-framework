﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PkmnFoundations.Data;
using PkmnFoundations.Structures;
using PkmnFoundations.Support;

namespace PkmnFoundations.Pokedex
{
    public class Pokedex
    {
        public Pokedex(Database db, bool lazy)
        {
            if (lazy) throw new NotImplementedException();

            GetAllData(db);
            BuildAdditionalIndexes();
            PrefetchRelations();
        }

        private void GetAllData(Database db)
        {
            m_species = db.PokedexGetAllSpecies(this).ToDictionary(s => s.NationalDex, s => s);
            m_families = db.PokedexGetAllFamilies(this).ToDictionary(f => f.ID, f => f);
            m_forms = db.PokedexGetAllForms(this).ToDictionary(f => f.ID, f => f);
            m_items = db.PokedexGetAllItems(this).ToDictionary(i => i.ID, i => i);
            m_moves = db.PokedexGetAllMoves(this).ToDictionary(m => m.ID, m => m);
            m_types = db.PokedexGetAllTypes(this).ToDictionary(t => t.ID, t => t);
            m_abilities = db.PokedexGetAllAbilities(this).ToDictionary(a => a.Value, a => a);
            m_ribbons = db.PokedexGetAllRibbons(this).ToDictionary(r => r.ID, r => r);
            m_regions = db.PokedexGetAllRegions(this).ToDictionary(r => r.ID, r => r);
            m_locations = db.PokedexGetAllLocations(this).ToDictionary(l => l.ID, l => l);

            List<FormStats> form_stats = db.PokedexGetAllFormStats(this);
            form_stats.Sort(delegate(FormStats f, FormStats other) 
            { 
                if (f.FormID != other.FormID) return f.FormID.CompareTo(other.FormID); 
                return f.MinGeneration.CompareTo(other.MinGeneration); 
            });

            Dictionary<int, SortedList<Generations, FormStats>> resultFormStats = new Dictionary<int, SortedList<Generations, FormStats>>();
            SortedList<Generations, FormStats> currFormStats = null;
            int currFormId = 0;

            foreach (FormStats f in form_stats)
            {
                if (currFormStats == null || currFormId != f.FormID)
                {
                    if (currFormStats != null) resultFormStats.Add(currFormId, currFormStats);
                    currFormStats = new SortedList<Generations, FormStats>();
                }
                currFormStats.Add(f.MinGeneration, f);
                currFormId = f.FormID;
            }
            if (currFormStats != null) resultFormStats.Add(currFormId, currFormStats);
            m_form_stats = resultFormStats;
        }

        private void BuildAdditionalIndexes()
        {
            m_forms_by_value = new Dictionary<int, Dictionary<byte, Form>>();
            foreach (var pair in m_species)
                m_forms_by_value.Add(pair.Key, new Dictionary<byte, Form>());

            foreach (var pair in m_forms)
            {
#if !DEBUG
                if (!m_forms_by_value[pair.Value.SpeciesID].ContainsKey(pair.Value.Value))
#endif
                    m_forms_by_value[pair.Value.SpeciesID].Add(pair.Value.Value, pair.Value);
            }

            Dictionary<int, Item> items3 = new Dictionary<int,Item>();
            Dictionary<int, Item> items4 = new Dictionary<int,Item>();
            Dictionary<int, Item> items5 = new Dictionary<int,Item>();
            Dictionary<int, Item> items6 = new Dictionary<int,Item>();
            m_items_generations = new Dictionary<Generations, Dictionary<int, Item>>();
            m_items_generations.Add(Generations.Generation3, items3);
            m_items_generations.Add(Generations.Generation4, items4);
            m_items_generations.Add(Generations.Generation5, items5);
            m_items_generations.Add(Generations.Generation6, items6);

            foreach (var pair in m_items)
            {
                Item i = pair.Value;
                if (i.Value3 != null) items3.Add((int)i.Value3, i);
                if (i.Value4 != null) items4.Add((int)i.Value4, i);
                if (i.Value5 != null) items5.Add((int)i.Value5, i);
                if (i.Value6 != null) items6.Add((int)i.Value6, i);
            }

            m_ribbon_positions_generations = new Dictionary<Generations, Dictionary<int, Ribbon>>();
            AddGeneration(m_ribbon_positions_generations, m_ribbons, Generations.Generation3, r => r.Position3);
            AddGeneration(m_ribbon_positions_generations, m_ribbons, Generations.Generation4, r => r.Position4);
            AddGeneration(m_ribbon_positions_generations, m_ribbons, Generations.Generation5, r => r.Position5);
            AddGeneration(m_ribbon_positions_generations, m_ribbons, Generations.Generation6, r => r.Position6);

            m_ribbon_values_generations = new Dictionary<Generations, Dictionary<int, Ribbon>>();
            AddGeneration(m_ribbon_values_generations, m_ribbons, Generations.Generation3, r => r.Value3);
            AddGeneration(m_ribbon_values_generations, m_ribbons, Generations.Generation4, r => r.Value4);
            AddGeneration(m_ribbon_values_generations, m_ribbons, Generations.Generation5, r => r.Value5);
            AddGeneration(m_ribbon_values_generations, m_ribbons, Generations.Generation6, r => r.Value6);

            m_location_values_generations = new Dictionary<LocationNumbering, Dictionary<int, Location>>();
            AddGeneration(m_location_values_generations, m_locations, LocationNumbering.Generation3, l => l.Value3);
            AddGeneration(m_location_values_generations, m_locations, LocationNumbering.Generation4, l => l.Value4);
            AddGeneration(m_location_values_generations, m_locations, LocationNumbering.Generation5, l => l.Value5);
            AddGeneration(m_location_values_generations, m_locations, LocationNumbering.Generation6, l => l.Value6);
        }

        private void AddGeneration<TGen, TKey, TValue>(Dictionary<TGen, Dictionary<TKey, TValue>> dest, Dictionary<TKey, TValue> src, TGen generation, Func<TValue, TKey?> keyGetter)
            where TKey : struct
        {
            dest.Add(generation,
                src.Where(pair => keyGetter(pair.Value) != null)
                .ToDictionary(pair => (TKey)keyGetter(pair.Value), pair => pair.Value));
        }

        private void PrefetchRelations()
        {
            // xxx: clean this up
            // todo: reflect these classes to decide whether or not prefetching
            // is even needed
            foreach (var k in m_species)
                k.Value.PrefetchRelations();
            foreach (var k in m_families)
                k.Value.PrefetchRelations();
            foreach (var k in m_forms)
                k.Value.PrefetchRelations();
            foreach (var k in m_items)
                k.Value.PrefetchRelations();
            foreach (var k in m_moves)
                k.Value.PrefetchRelations();
            foreach (var k in m_types)
                k.Value.PrefetchRelations();
            foreach (var k in m_abilities)
                k.Value.PrefetchRelations();
            foreach (var k in m_ribbons)
                k.Value.PrefetchRelations();
            foreach (var k in m_regions)
                k.Value.PrefetchRelations();
            foreach (var k in m_locations)
                k.Value.PrefetchRelations();

            foreach (var k in m_form_stats)
            {
                foreach (var j in k.Value)
                    j.Value.PrefetchRelations();
            }
        }

        private Dictionary<int, Species> m_species;
        private Dictionary<int, Family> m_families;
        private Dictionary<int, Form> m_forms;
        private Dictionary<int, Dictionary<byte, Form>> m_forms_by_value;
        private Dictionary<int, SortedList<Generations, FormStats>> m_form_stats;
        //private Dictionary<int, Evolution> m_evolutions;

        private Dictionary<int, Item> m_items;
        private Dictionary<int, Move> m_moves;
        private Dictionary<int, PkmnFoundations.Pokedex.Type> m_types;
        private Dictionary<int, Ability> m_abilities;
        private Dictionary<int, Ribbon> m_ribbons;

        private Dictionary<Generations, Dictionary<int, Item>> m_items_generations;
        private Dictionary<Generations, Dictionary<int, Ribbon>> m_ribbon_positions_generations;
        private Dictionary<Generations, Dictionary<int, Ribbon>> m_ribbon_values_generations;

        private Dictionary<int, Region> m_regions;
        private Dictionary<int, Location> m_locations;

        private Dictionary<LocationNumbering, Dictionary<int, Location>> m_location_values_generations;

        // todo: use readonly wrappers
        public IDictionary<int, Species> Species
        {
            get
            {
                return m_species;
            }
        }

        public Family Families(int id)
        {
            return m_families[id];
        }

        public Form Forms(int id)
        {
            return m_forms[id];
        }

        internal Dictionary<byte, Form> FormsByValue(int national_dex)
        {
            return m_forms_by_value[national_dex];
        }

        internal SortedList<Generations, FormStats> FormStats(int form_id)
        {
            return m_form_stats[form_id];
        }

        public Item Items(int id)
        {
            return m_items[id];
        }

        public Item Items(Generations generation, int value)
        {
            return m_items_generations[generation][value];
        }

        public Item Pokeballs(int value)
        {
            // fixme: fact check the values for apricorn pokeballs.
            // What's used here is most assuredly wrong (and probably quite silly)
            // todo: add a PokeballValue field to the Items table and a dictionary here
            return m_items_generations[Generations.Generation5][value];
        }

        public Move Moves(int value)
        {
            return m_moves[value];
        }

        public PkmnFoundations.Pokedex.Type Types(int id)
        {
            return m_types[id];
        }

        public Ability Abilities(int value)
        {
            return m_abilities[value];
        }

        public IDictionary<int, Ribbon> Ribbons()
        {
            return m_ribbons;
        }

        public IDictionary<int, Ribbon> Ribbons(Generations generation)
        {
            return m_ribbon_positions_generations[generation];
        }

        public Region Regions(int id)
        {
            return m_regions[id];
        }

        public Location Locations(int id)
        {
            return m_locations[id];
        }

        public IDictionary<int, Location> Locations(LocationNumbering generation)
        {
            return m_location_values_generations[generation];
        }

        public static int SpeciesAtGeneration(Generations generation)
        {
            switch (generation)
            {
                case Generations.Generation1:
                    return 151;
                case Generations.Generation2:
                    return 251;
                case Generations.Generation3:
                    return 386;
                case Generations.Generation4:
                    return 493;
                case Generations.Generation5:
                    return 649;
                case Generations.Generation6:
                    return 721;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
