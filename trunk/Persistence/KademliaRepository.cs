﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using Persistence.Tag;
using System.Threading.Tasks;
using System.Threading;
using Raven.Client.Indexes;
using Raven.Database.Indexing;
using Raven.Client.Document;
using System.Text.RegularExpressions;


/*
 * from key in docs.KademliaKeywords
 * from tag in key.Tags
 * select new { Kid = key.Id , Tid = tag}
 */

namespace Persistence
{
    public class KademliaRepository:IDisposable
    {
        public const string DefaultSemanticFilterRegexString=@"\b("+
                                                              "the|a|an|"+//English Articles
                                                              "for|and|nor|but|or|yet|so|of|to|" + //English Coordinating conjunction
                                                              "both|either|neither|rather|whetever|" + //English Correlative Conjunction (not all)
                                                              "as|although|for|if|so|than|unless|until|till|while|" +//English Subordinate Conjunctions (not all)
                                                              "lo|il|la|i|gli|le|l'|"+//Italian Articles
                                                              "di|a|da|in|con|su|per|fra|tra|"+//Italian Prepositions
                                                              "e|anche|pure|inoltre|ancora|perfino|neanche|neppure|nemmeno|"+ //Italian Conjunctions
                                                              "oppure|altrimenti|ovvero|ossia|dunque|quindi|pertanto|allora|infatti|difatti|invero|"+ //Italian Conjunctions (Continue)
                                                              "che|mentre|se|"+//Italian Conjunctions (Continue)
                                                              "un|une|des|de|"+ //French articles
                                                              "et|ou|ni|mais|donc"+ //French Conjunction (car omitted to avoid clash with english terms)
                                                              @")\b";
        private static readonly ILog log = LogManager.GetLogger(typeof(KademliaRepository));
        private Repository _repository;
        private Regex _semanticRegex;
        private Regex _whiteSpaceRegex;
        public KademliaRepository(string repType="Raven",
                                  RepositoryConfiguration conf=null,
                                  string semanticFilter=KademliaRepository.DefaultSemanticFilterRegexString) 
        {
            log.Debug("Semantic Filter Regex used is "+DefaultSemanticFilterRegexString);
            this._semanticRegex = new Regex(DefaultSemanticFilterRegexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            this._whiteSpaceRegex = new Regex(@"[ ]{2,}", RegexOptions.Compiled);
            this._repository = RepositoryFactory.GetRepositoryInstance(repType, conf);
            this._repository.CreateIndex("KademliaKeywords/KeysByTag",
                                         "from key in docs.KademliaKeywords\nfrom tag in key.Tags\nselect new { Kid = key.Id , Tid = tag}");
            this._repository.CreateIndex("KademliadKeywords/EmptyKeys",
                                         "from key in docs.KademliaKeywords\nwhere key.Tags.Count() == 0\nselect new { key.Id }");
        }
        public bool StoreResource(CompleteTag tag, Uri peer)
        {
            KademliaResource rs = new KademliaResource();
            RepositoryResponse resp = _repository.GetByKey<KademliaResource>(tag.TagHash, rs);
            if ( resp == RepositoryResponse.RepositoryLoad)
            {
                if (!rs.Urls.Contains(peer)) {
                    _repository.ArrayAddElement(rs.Id, "Urls", peer);
                } else {
                    log.Debug("Urls "+peer.ToString()+" already known");
                }
            }
            else if (resp == RepositoryResponse.RepositoryMissingKey)
            {
                rs = new KademliaResource(tag, peer);
                if (_repository.Save(rs) == RepositoryResponse.RepositorySuccess)
                {
                    List<string> pks = new List<string>(generatePrimaryKey(tag));
                    List<KademliaKeyword> keys = new List<KademliaKeyword>();
                    if (_repository.GetByKeySet(pks.ToArray(), keys) > 0)
                    {
                        foreach (KademliaKeyword k in keys)
                        {
                            if (!k.Tags.Contains(rs.Id))
                            {
                                _repository.ArrayAddElement(k.Id, "Tags", rs.Id);
                            }
                            pks.Remove(k.Id);
                        }
                        foreach (String pk in pks)
                        {
                            KademliaKeyword localKey = new KademliaKeyword(pk,rs.Id);
                            _repository.Save(localKey);                            
                        }
                    }
                    else
                    {
                        log.Error("Unexpected reposnde while getting keywords");
                        return false;
                    }

                }
                else
                {
                    log.Error("Unexpected response while inserting Tag with key " + tag.TagHash);
                    return false;
                }
            }
            else
            {
                log.Error("Unexpected response while testing presence of the key " + tag.TagHash);
                return false;
            }
            return true;
        }
        public KademliaResource[] SearchFor(string query)
        {
            List<KademliaResource> resources = new List<KademliaResource>();
            List<KademliaKeyword> keys = new List<KademliaKeyword>();
            string[] queryParts = query.Split(' ');
            _repository.GetAllByCondition(kw =>
            {
                string kid=kw.Id.Substring(17);
                foreach (string p in queryParts) {
                    if (kid.Contains(p.ToLower()))
                    {
                        return true;
                    }
                }
                return false;
            }, keys);
            List<string> tids = new List<string>();
            foreach (KademliaKeyword kw in keys)
            {
                tids.AddRange(kw.Tags);
            }
            _repository.GetByKeySet(tids.ToArray(), resources);
            return resources.ToArray();
        }
        public bool DeleteTag(string tid)
        {
            List<KademliaKeyword> results = new List<KademliaKeyword>();
            this._repository.QueryOverIndex("KademliaKeywords/KeysByTag", "Tid:"+tid, results);
            foreach (var t in results)
            {
                //t.Tags.FindIndex(x => x.Equals(tid))
                this._repository.ArrayRemoveElement(t.Id,"Tags",tid);
            }
            this._repository.Delete<KademliaResource>(tid);
            results.Clear();
            this._repository.QueryOverIndex("KademliaKeywords/EmptyKeys", "",results);
            string[] ids = new string[results.Count];
            int index = 0;
            foreach (var t in results)
            {
                ids[index++] = t.Id;
            }
            this._repository.BulkDelete<KademliaKeyword>(ids);
            return true;
        }
        public string DiscardSemanticlessWords(string str)
        {
            return _whiteSpaceRegex.Replace(_semanticRegex.Replace(str,"")," ").Trim();
        }
        private string[] generatePrimaryKey(CompleteTag tag)
        {
            List<string> pkList = new List<string>();            
            StringBuilder sb = new StringBuilder();
            sb.Append(tag.Title);
            sb.Append(" ");
            sb.Append(tag.Artist);
            sb.Append(" ");
            sb.Append(tag.Album);
            string[] keywords = DiscardSemanticlessWords(sb.ToString()).Split(' ');
            UTF8Encoding enc = new UTF8Encoding();
            //Questo è solo un esercizio di stile !
            /*Parallel.ForEach<string,List<string> >(keywords, 
                                                    () => new List<string>(),                                    
                                                    (key,loop,sublist)  =>
                                                    {
                                                        sublist.Add(key);
                                                        return sublist;
                                                    },
                                                    (finalResult) => pkList.AddRange(finalResult)
            );*/
            for (int k = 0; k < keywords.Length; k++)
            {
                string key = null;
                if (keywords[k].Length > 32)
                {
                    key = keywords[k].Substring(0, 32).ToLower();
                }
                else
                {
                    key = keywords[k].ToLower();
                }
                if (!pkList.Contains(key)) pkList.Add("kademliakeywords/"+key);
            }
            return pkList.ToArray();
        }


        #region IDisposable

        public void Dispose()
        {
            _repository.Dispose();
        }

        #endregion
    }
}