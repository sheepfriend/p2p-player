/*****************************************************************************************
 *  p2p-player
 *  An audio player developed in C# based on a shared base to obtain the music from.
 * 
 *  Copyright (C) 2010-2011 Dario Mazza, Sebastiano Merlino
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as
 *  published by the Free Software Foundation, either version 3 of the
 *  License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *  
 *  Dario Mazza (dariomzz@gmail.com)
 *  Sebastiano Merlino (etr@pensieroartificiale.com)
 *  Full Source and Documentation available on Google Code Project "p2p-player", 
 *  see <http://code.google.com/p/p2p-player/>
 *
 ******************************************************************************************/

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence
{
    public class TrackRepository: IDisposable
    {
        private Repository _repository;
        public TrackRepository(string repType,RepositoryConfiguration conf) {
            this._repository = RepositoryFactory.GetRepositoryInstance(repType, conf);
        }
        public RepositoryResponse InsertTrack(string filename) {
            if (System.IO.File.Exists(filename)) {
                TrackModel tk=new TrackModel(filename);
                RepositoryResponse rsp=this._repository.Save(tk);
                if (rsp>=0) {
                    return RepositoryResponse.RepositoryInsert;
                } else {
                    return rsp;
                }
            } else if(System.IO.Directory.Exists(filename)) {
                string[] files=System.IO.Directory.GetFiles(filename,"*.mp3");
                Parallel.ForEach(files,file => {
                    TrackModel tk = new TrackModel(file);
                    this._repository.Save(tk);
                });
                return RepositoryResponse.RepositoryInsert;
            } else {
                return RepositoryResponse.RepositoryGenericError;
            }
        }
        public RepositoryResponse InsertTrack(TrackModel mdl) {
            RepositoryResponse rsp = this._repository.Save(mdl);
            if (rsp >= 0 ) {
                return RepositoryResponse.RepositoryInsert;
            } else {
                return rsp;
            }
        }
        public RepositoryResponse Delete(string key) {
            return this._repository.Delete<TrackModel.Track>(key);
        }
        public RepositoryResponse Update(TrackModel mdl) {
            return this._repository.Save(mdl);
        }
        public TrackModel Get(string key) {
            TrackModel tk = new TrackModel();
            if (this._repository.GetByKey<TrackModel.Track>(key, tk) >= 0)
            {
                return tk;
            }
            else
            {
                return null;
            }
        }
        public ICollection<TrackModel> GetAll() {
            LinkedList<TrackModel> list = new LinkedList<TrackModel>();
            LinkedList<TrackModel.Track> dbList= new LinkedList<TrackModel.Track>();
            RepositoryResponse rsp = this._repository.GetAll(dbList);
            Parallel.ForEach(dbList, elem =>
            {
                TrackModel tk = new TrackModel();
                tk.LoadFromDatabaseType(elem);
                list.AddLast(tk);
            });
            return list;
        }
        public int Count() {
            return this._repository.Count<TrackModel.Track>();
        }

        #region IDisposable

        public void Dispose()
        {
            this._repository.Dispose();
        }

        #endregion
    }
}// namespace Persistence
