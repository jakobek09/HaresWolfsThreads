using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Threading;
using System.Windows.Controls;
using System.Reflection.Emit;
using Label = System.Windows.Controls.Label;
using System.Diagnostics.Eventing.Reader;

namespace zajace
{
	public partial class MainWindow : Window
	{
		static public int numOfWolfs = 3; // Number of wolves
		static public int numOfHares = 15; // Number of hares

		static public Label label2 = new Label();
		static public Label label3 = new Label();


		class Animal
		{
			public int posy; // Y-coordinate position of the animal
			public int posx; // X-coordinate position of the animal
			public int speed; // Speed of the animal
			public Ellipse ellipse; // Ellipse representing the animal

			public void Draw(Canvas canvas)
			{
				ellipse = new Ellipse();
				ellipse.Width = 20;
				ellipse.Height = 20;
				ellipse.Fill = Brushes.Black;
				canvas.Children.Add(ellipse);

				Canvas.SetLeft(ellipse, posx);
				Canvas.SetTop(ellipse, posy);
			}
		}

		class Wolf : Animal
		{
			public List<Label> labels = new List<Label>(); // List of labels associated with the wolf
			public bool isChasing = false; // Flag indicating whether the wolf is chasing a hare
			public string hareID2 = ""; // ID of the chased hare
			private Barrier barrier; // Barrier for synchronization
			public int points = 0; // Points acquired by the wolf
			int start = 0; // Flag indicating the start of the wolf's execution

			public Wolf(int _posy, int _posx, int _speed, Barrier _barrier)
			{
				posy = _posy;
				posx = _posx;
				speed = _speed;
				barrier = _barrier;
			}

			public new void Draw(Canvas canvas, int x)
			{
				base.Draw(canvas);

				Label label = new Label();
				label.Foreground = Brushes.White;
				canvas.Children.Add(label);
				Canvas.SetLeft(label, posx);
				Canvas.SetTop(label, posy);
				labels.Add(label);
				updateLabel(hareID2, x);
			}

			public void chaseHare(Hare hare)
			{
				int hareX = hare.posx;
				int hareY = hare.posy;

				if (posx < hareX)
					posx += speed;
				else if (posx > hareX)
					posx -= speed;

				if (posy < hareY)
					posy += speed;
				else if (posy > hareY)
					posy -= speed;
			}

			public void updateLabel(string hareID, int x)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					label2.Content = "Hares alive: " + x;
					foreach (Label label in labels)
					{
						label.Content = hareID + "/" + points;
					}
				});

			}

			public void wolfFunc(ref List<Hare> hares)
			{
				Random rand = new Random();
				int randIndex;
				Hare hare = null;
				while (true)
				{
					lock (hares) // Synchronize access to the list of hares
					{
						// Check if the wolf is still chasing a hare
						if (!isChasing && hares.Count > numOfWolfs - 1)
						{
							do
							{
								randIndex = rand.Next(hares.Count);
							} while (hares[randIndex].isChased == true);

							hare = hares[randIndex];
							hareID2 = hare.id.ToString();
							hare.isChased = true;
							isChasing = true;
						}
						else if (!isChasing && hares.Count <= numOfWolfs - 1)
						{
							this.speed = 0;
							break;
						}

						//Thread.Sleep(3000);
					}
					if (this.start == 0)
					{
						barrier.SignalAndWait(); // Wait for the other wolves
						this.start = 1;
					}
					chaseHare(hare);

					Thread.Sleep(100);
				}
			}
		}

		class Hare : Animal
		{
			static int count = 1;
			public int id = 0;
			public bool isChased { get; set; } = false;

			private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

			public Hare(int _posy, int _posx, int _speed)
			{
				id = count;
				count++;
				posy = _posy;
				posx = _posx;
				speed = _speed;
			}

			public void Move(int canvasWidth, int canvasHeight, ref List<Wolf> wolves, ref List<Hare> hares)
			{
				while (true)
				{
					Random rand = new Random();
					int direction = rand.Next(0, 4);
					int distance = 10;

					switch (direction)
					{
						case 0:
							posy -= speed;
							if (posy < 0)
								posy = 20;
							break;
						case 1:
							posy += speed;
							if (posy > canvasHeight - 30)
								posy = canvasHeight - 30;
						break;
						case 2:
							posx -= speed;
							if (posx < 0)
								posx = 20;
							break;
						case 3:
							posx += speed;
							if (posx > canvasWidth - 30)
								posx = canvasWidth - 30;
							break;
					}
					List<Wolf> wolvesCopy;
					lock (wolves)
					{
						wolvesCopy = new List<Wolf>(wolves);
					}
					foreach (Wolf w in wolvesCopy)
					{
						int wolfX = w.posx;
						int wolfY = w.posy;

						int distFromWolfX = Math.Abs(posx - wolfX);
						int distFromWolfY = Math.Abs(posy - wolfY);

						if (distFromWolfX <= distance || distFromWolfY <= distance)
						{
							if (distFromWolfX <= 1 && distFromWolfY <= 1)
							{
								semaphore.Wait();
								cought(w, this, ref hares);
								semaphore.Release();
							}
							if (posx < wolfX)
								posx -= speed;
							else if (posx > wolfX)
								posx += speed;

							if (posy < wolfY)
								posy -= speed;
							else if (posy > wolfY)
								posy += speed;
						}
					}
					Thread.Sleep(100);
				}
			}

			public void cought(Wolf w, Hare h, ref List<Hare> hares)
			{
				Random rand2 = new Random();
				int x = rand2.Next(2);
				lock (w)
				{
					if (int.Parse(w.hareID2) == this.id)
					{
						lock (hares)
						{
							if (hares.Contains(h))
							{
								hares.Remove(h);
								w.points++;
								w.isChasing = false;
							}
						}
					}
				}
			}

			public new void Draw(Canvas canvas)
			{
				base.Draw(canvas);
				ellipse.Fill = Brushes.Gray;

				// Add ID as text on ellipse
				TextBlock textBlock = new TextBlock();
				textBlock.Text = id.ToString();
				textBlock.Foreground = Brushes.White;
				canvas.Children.Add(textBlock);
				Canvas.SetLeft(textBlock, posx + 5);
				Canvas.SetTop(textBlock, posy);
			}
		}

		List<Hare> hares = new List<Hare>();
		List<Wolf> wolves = new List<Wolf>();
		Barrier barrier;

		public MainWindow()
		{
			
			InitializeComponent();
			Loaded += MainWindow_Loaded;

			label3.Content = "Wolfs: " + numOfWolfs;
			Canvas.SetLeft(label3, 100);
			Canvas.SetTop(label3, 0);

		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			int a = (int)canvas.ActualWidth;
			int b = (int)canvas.ActualHeight;
			int x = 50;
			int y = 50;

			for (int i = 0; i < numOfHares; i++)
			{
				Hare hare1 = new Hare(y, x, 5);
				hares.Add(hare1);

				Thread hareThread = new Thread(() => hare1.Move(a, b, ref wolves, ref hares));
				hareThread.Start();
				x += 30;
				y += 40;
			}

			int wolfX = 500;
			int wolfY = 300;

			barrier = new Barrier(numOfWolfs);

			for (int i = 0; i < numOfWolfs; i++)
			{
				Wolf wolf = new Wolf(wolfY, wolfX, 6, barrier);
				wolves.Add(wolf);

				Thread wolfThread = new Thread(() => wolf.wolfFunc(ref hares));
				wolfThread.Start();
				wolfX += 10;
				wolfY += 10;
			}

			Thread drawThread = new Thread(new ThreadStart(DrawAnimals));
			drawThread.Start();
		}

		
		private void DrawAnimals()
		{	
			while (true)
			{
				Dispatcher.Invoke(() =>
				{
					lock (hares)
					{
						canvas.Children.Clear();
						foreach (Hare hare in hares)
						{
							hare.Draw(canvas);
						}
					}
					foreach (Wolf wolf in wolves)
					{
						wolf.Draw(canvas, hares.Count);
					}
					canvas.Children.Add(label2);
					canvas.Children.Add(label3);
				});

				Thread.Sleep(50);
			}
		}
	}
}
