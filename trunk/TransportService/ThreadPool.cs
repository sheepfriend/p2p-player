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

namespace TransportService
{
    internal class ThreadPool
    {
        private ThreadPoolObject[] pool;
        private int poolSize;

        public ThreadPool()
        {
        }

        public ThreadPool(int poolSize)
        {
            this.poolSize = poolSize;
            this.pool = new ThreadPoolObject[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                this.pool[i] = new ThreadPoolObject();
            }
        }

        public ThreadPoolObject GetNextThreadInPool()
        {
            return this.pool.AsParallel().Aggregate((l, r) => l.State == ThreadPoolObject.ThreadState.FREE ? l : r);
        }
    }
}
