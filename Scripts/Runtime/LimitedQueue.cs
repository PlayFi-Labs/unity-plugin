using System.Collections.Generic;

namespace PlayFi
{
	internal class LimitedQueue<T> : Queue<T>
	{
		private int Limit { get; set; }

		internal LimitedQueue(int limit) : base(limit)
		{
			Limit = limit;
		}

		internal new void Enqueue(T item)
		{
			while (Count >= Limit)
			{
				Dequeue();
			}
			base.Enqueue(item);
		}
	}
}