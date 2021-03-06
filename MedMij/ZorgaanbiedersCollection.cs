// Copyright (c) Zorgdoc.  All rights reserved.  Licensed under the AGPLv3.

namespace MedMij
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.Schema;

    /// <summary>
    /// Een zorgaanbiederslijst zoals beschreven op https://afsprakenstelsel.medmij.nl/
    /// </summary>
    public class ZorgaanbiedersCollection : IEnumerable<Zorgaanbieder>
    {
        private static readonly XNamespace NS = "xmlns://afsprakenstelsel.medmij.nl/zorgaanbiederslijst/release2/";
        private static readonly XName ZorgaanbiederslijstRoot = NS + "Zorgaanbiederslijst";
        private static readonly XName ZorgaanbiederName = NS + "Zorgaanbieder";
        private static readonly XName ZorgaanbiedernaamName = NS + "Zorgaanbiedernaam";
        private static readonly XName GegevensdienstName = NS + "Gegevensdienst";
        private static readonly XName GegevensdienstIdName = NS + "GegevensdienstId";
        private static readonly XName AuthorizationEndpointuriName = NS + "AuthorizationEndpointuri";
        private static readonly XName TokenEndpointuriName = NS + "TokenEndpointuri";

        private static readonly XmlSchemaSet Schemas = XMLUtils.SchemaSetFromResource("Zorgaanbiederslijst.xsd", NS);

        private readonly IReadOnlyDictionary<string, Zorgaanbieder> dict;

        private ZorgaanbiedersCollection(XDocument doc)
        {
            XMLUtils.Validate(doc, Schemas, ZorgaanbiederslijstRoot);
            this.dict = Parse(doc);
        }

        /// <summary>
        /// Initialiseert een <see cref="ZorgaanbiedersCollection"/> vanuit een string. Parset de string and valideert deze.
        /// </summary>
        /// <param name="xmlData">Een string met de zorgaanbiederslijst als XML.</param>
        /// <returns>De nieuwe <see cref="ZorgaanbiedersCollection"/>.</returns>
        public static ZorgaanbiedersCollection FromXMLData(string xmlData)
        {
            var doc = XDocument.Parse(xmlData);
            return new ZorgaanbiedersCollection(doc);
        }

        /// <summary>
        /// Geeft de <see cref="Zorgaanbieder"/> met de opgegeven naam.
        /// </summary>
        /// <param name="name">De naam van de <see cref="Zorgaanbieder"/></param>
        /// <returns>De gezochte <see cref="Zorgaanbieder"/>.</returns>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Wordt gegenereerd als de naam niet wordt gevonden.</exception>
        public Zorgaanbieder GetByName(string name) => this.dict[name];

        /// <summary>
        /// Returnt een enumerator die door de <see cref="Zorgaanbieder"/>s itereert.
        /// </summary>
        /// <returns>De <see cref="IEnumerator"/>.</returns>
        IEnumerator<Zorgaanbieder> IEnumerable<Zorgaanbieder>.GetEnumerator() => this.dict.Values.GetEnumerator();

        /// <summary>
        /// Returnt een enumerator die door de <see cref="Zorgaanbieder"/>s itereert.
        /// </summary>
        /// <returns>De <see cref="IEnumerator"/>.</returns>
        IEnumerator IEnumerable.GetEnumerator() => this.dict.Values.GetEnumerator();

        private static IReadOnlyDictionary<string, Zorgaanbieder> Parse(XDocument doc)
        {
            Gegevensdienst ParseGegevensdienst(XElement x, string zorgaanbiedernaam)
            {
                var id = x.Element(GegevensdienstIdName).Value;
                var authorizationEndpointUri = x.Descendants(AuthorizationEndpointuriName).Single().Value;
                var tokenEndpointUri = x.Descendants(TokenEndpointuriName).Single().Value;
                return new Gegevensdienst(
                    id: id,
                    zorgaanbiedernaam: zorgaanbiedernaam,
                    authorizationEndpointUri: new Uri(authorizationEndpointUri),
                    tokenEndpointUri: new Uri(tokenEndpointUri));
            }

            Zorgaanbieder ParseZorgaanbieder(XElement x)
            {
                var naam = x.Element(ZorgaanbiedernaamName).Value;
                var gegevensdiensten = x.Descendants(GegevensdienstName)
                                        .Select(e => ParseGegevensdienst(e, naam))
                                        .ToDictionary(g => g.Id, g => g);
                return new Zorgaanbieder(naam: naam, gegevensdiensten: gegevensdiensten);
            }

            var zorgaanbieders = doc.Descendants(ZorgaanbiederName).Select(ParseZorgaanbieder);
            var d = zorgaanbieders.ToDictionary(z => z.Naam, z => z);
            return new ReadOnlyDictionary<string, Zorgaanbieder>(d);
        }
    }
}
