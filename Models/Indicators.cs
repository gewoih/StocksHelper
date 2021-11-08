﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace StocksHelper.Models
{
	public static class Indicators
	{
		public static double CalculateCCI(List<DataPoint> quotes, int period)
		{
			//Индекс для удобства
			int index = quotes.Count;
			if (index > period)
			{
				//Вычисляем среднее значение типичной цены
				double avgTp = 0;
				for (int p = index - period; p < index; p++)
				{
					DataPoint d = quotes[p];
					avgTp += d.Y;
				}
				avgTp /= period;

				//Вычисляем среднее значение отклонений цены
				double avgDv = 0;
				for (int p = index - period; p < index; p++)
				{
					DataPoint d = quotes[p];
					avgDv += Math.Abs(avgTp - d.Y);
				}
				avgDv /= period;

				//Добавляем новое значение CCI по формуле
				return (quotes[index-1].Y - avgTp) / (0.015 * avgDv);
			}
			return 0;
		}

		public static double CalculateRSI(List<DataPoint> quotes, int period)
		{
			double lastValue = quotes.Last().Y;
			double avgGain = 0;
			double avgLoss = 0;

			int size = quotes.Count;
			List<DataPoint> results = new(size);
			double[] gain = new double[size]; // gain
			double[] loss = new double[size]; // loss

			for (int i = 0; i < quotes.Count; i++)
			{
				//Заполняем массив восходящих и нисходящих цен
				DataPoint h = quotes[i];
				int index = i + 1;

				DataPoint RSIResult = new DataPoint(h.X, h.Y);

				gain[i] = (h.Y > lastValue) ? h.Y - lastValue : 0;
				loss[i] = (h.Y < lastValue) ? lastValue - h.Y : 0;
				lastValue = h.Y;

				//Вычисляем RSI
				if (index > period + 1)
				{
					avgGain = (avgGain * (period - 1) + gain[i]) / period;
					avgLoss = (avgLoss * (period - 1) + loss[i]) / period;

					if (avgLoss > 0)
					{
						double rs = avgGain / avgLoss;
						RSIResult = new DataPoint(RSIResult.X, 100 - (100 / (1 + rs)));
					}
					else
						RSIResult = new DataPoint(RSIResult.X, 100);
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

					RSIResult = new DataPoint(RSIResult.X, (avgLoss > 0) ? 100 - (100 / (1 + (avgGain / avgLoss))) : 100);
				}
				results.Add(RSIResult);
			}

			return results.Last().Y;
		}
	}
}
