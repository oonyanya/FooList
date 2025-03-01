/*
 * Copyright (C) 2013 FooProject
 * * This program is free software; you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program. If not, see <http://www.gnu.org/licenses/>.
 */
#region Using Directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

#endregion Using Directives


namespace Slusser.Collections.Generic
{
	internal sealed class CollectionDebugView<T>
	{
		#region Fields

		private ICollection<T> _collection;

		#endregion Fields


		#region Constructors

		public CollectionDebugView(ICollection<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException("collection");

			this._collection = collection;
		}

		#endregion Constructors


		#region Properties

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				T[] array = new T[this._collection.Count];
				this._collection.CopyTo(array, 0);
				return array;
			}
		}

		#endregion Properties
	}
}
