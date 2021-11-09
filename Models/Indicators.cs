using System;
using System.Collections.Generic;
using System.Linq;

namespace StocksHelper.Models
{
	public static class Indicators
	{
		public static double CalculateCCI(List<StockQuote> quotes, int period)
		{
			//Индекс для удобства
			int index = quotes.Count;
			if (index > period)
			{
				//Вычисляем среднее значение типичной цены
				double avgTp = 0;
				for (int p = index - period; p < index; p++)
				{
					StockQuote d = quotes[p];
					avgTp += d.ClosePrice;
				}
				avgTp /= period;

				//Вычисляем среднее значение отклонений цены
				double avgDv = 0;
				for (int p = index - period; p < index; p++)
				{
					StockQuote d = quotes[p];
					avgDv += Math.Abs(avgTp - d.ClosePrice);
				}
				avgDv /= period;

				//Добавляем новое значение CCI по формуле
				return (quotes[index-1].ClosePrice - avgTp) / (0.015 * avgDv);
			}
			return 0;
		}

		public static double CalculateRSI(List<StockQuote> quotes, int period)
		{
			double lastValue = quotes.Last().ClosePrice;
			double avgGain = 0;
			double avgLoss = 0;

			int size = quotes.Count;
			List<StockQuote> results = new(size);
			double[] gain = new double[size]; // gain
			double[] loss = new double[size]; // loss

			for (int i = 0; i < quotes.Count; i++)
			{
				//Заполняем массив восходящих и нисходящих цен
				StockQuote h = quotes[i];
				int index = i + 1;

				StockQuote RSIResult = new StockQuote { DateTime = h.DateTime, ClosePrice = h.ClosePrice };

				gain[i] = (h.ClosePrice > lastValue) ? h.ClosePrice - lastValue : 0;
				loss[i] = (h.ClosePrice < lastValue) ? lastValue - h.ClosePrice : 0;
				lastValue = h.ClosePrice;

				//Вычисляем RSI
				if (index > period + 1)
				{
					avgGain = (avgGain * (period - 1) + gain[i]) / period;
					avgLoss = (avgLoss * (period - 1) + loss[i]) / period;

					if (avgLoss > 0)
					{
						double rs = avgGain / avgLoss;
						RSIResult = new StockQuote { DateTime = RSIResult.DateTime, ClosePrice = 100 - (100 / (1 + rs)) };
					}
					else
						RSIResult = new StockQuote { DateTime = RSIResult.DateTime, ClosePrice = 100 };
				}

				//Расчет средней цены
				else if (index == period + 1)
				{
					double sumGain = 0;
					double sumLoss = 0;

					for (int p = 1; p <= period; p++)
					{
						sumGain += gain[p];
						sumLoss += loss[p];
					}
					avgGain = sumGain / period;
					avgLoss = sumLoss / period;

					RSIResult = new StockQuote { DateTime = RSIResult.DateTime, ClosePrice = (avgLoss > 0) ? 100 - (100 / (1 + (avgGain / avgLoss))) : 100 };
				}
				results.Add(RSIResult);
			}

			return results.Last().ClosePrice;
		}
	}
}
