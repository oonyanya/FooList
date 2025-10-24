using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FooProject.Collection.DataStore
{
    /// <summary>
    /// IPinableContainerStore<T>を実装した抽象クラス。カスタムデーターストアを実装する場合、なるべくこのクラスから継承するようにしてください。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class PinableContentDataStoreBase<T> : IPinableContainerStore<T>
    {
        /// <inheritdoc/>
        public virtual IPinableContainer<T> Clone(IPinableContainer<T> pin, T cloned_content)
        {
            return this.CreatePinableContainer(cloned_content);
        }

        /// <inheritdoc/>
        public virtual void Commit()
        {
        }

        /// <inheritdoc/>
        /// <remarks>必ず実装してください</remarks>
        public virtual IPinableContainer<T> CreatePinableContainer(T content)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual IPinnedContent<T> Get(IPinableContainer<T> pinableContainer)
        {
            IPinnedContent<T> result;
            if (TryGet(pinableContainer, out result))
                return result;
            else
                throw new ArgumentException();
        }

        /// <inheritdoc/>
        public virtual bool IsCanCloneContent(IPinableContainer<IComposableList<char>> pin)
        {
            return false;
        }

        /// <inheritdoc/>
        /// <remarks>必ず実装してください</remarks>
        public virtual void Set(IPinableContainer<T> pinableContainer)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        /// <remarks>必ず実装してください</remarks>
        public virtual bool TryGet(IPinableContainer<T> pinableContainer, out IPinnedContent<T> result)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual IPinableContainer<T> Update(IPinableContainer<T> pinableContainer, T newcontent, long oldstart, long oldcount, long newstart, long newcount)
        {
            return this.CreatePinableContainer(newcontent);
        }
    }
}
