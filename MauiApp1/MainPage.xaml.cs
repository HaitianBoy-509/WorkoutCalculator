namespace MauiApp1;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class MainPage : ContentPage
{

    private bool isRunning = false;
    private DateTime startTime;
    private TimeSpan elapsed;
    private IDispatcherTimer timer;
    private bool isPaused = false;
    public MainPage()
    {
        InitializeComponent();
        LoadHistory();
    }

    private void OnCalculateClicked(object sender, EventArgs e)
    {
        // Les deux champs poids doivent être remplis (pas vide, pas que des espaces)
        if (string.IsNullOrWhiteSpace(TotalWeightEntry.Text) ||
            string.IsNullOrWhiteSpace(BarWeightEntry.Text))

        {
            DisplayAlert("Erreur", "Remplis tous les champs", "OK");
            return;
        }

        // Texte de l'item sélectionné dans le picker (? = null si rien choisi)
        string goal = GoalPicker.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(goal)) // picker laissé sur "Choisir…"
        {
            DisplayAlert("Erreur", "Choisis un objectif", "OK");
            return;
        }

        double totalWeight = double.Parse(TotalWeightEntry.Text);
        double barWeight = double.Parse(BarWeightEntry.Text);



        // var exercises = GetExercises(goal);
        // ExerciseView.ItemsSource = exercises;

        var image = GetExerciseImage(goal);
        ExerciseImage.Source = image;

        ExerciseTitle.Text = goal;

        var exercises = GetExercises(goal);
        ExerciseView.ItemsSource = exercises;

        SaveSession(totalWeight, goal, barWeight);

        var sections = new List<CoachItem>
        {
            new CoachItem
            {
                Title = "🔥 Échauffement",
                Lines = BuildCoachWarmup(totalWeight, goal)
            },
            new CoachItem
            {
                Title = "💪 Travail",
                Lines = BuildCoachWork(totalWeight, goal)
            },
            new CoachItem
            {
                Title = "⚡ Conseils pro",
                Lines = BuildCoachAdvice(goal)
            }
        };

        ResultView.ItemsSource = sections;

        LoadHistory();

    }
    private static string Kg(double w) => $"{Math.Round(w)} kg";

    private List<string> BuildCoachWarmup(double totalWeight, string goal)
    {
        double w40 = Math.Round(totalWeight * 0.4);
        double w60 = Math.Round(totalWeight * 0.6);
        double w75 = Math.Round(totalWeight * 0.75);

        string tag = goal switch
        {
            "Force" => "🔋 Montée → charge lourde",
            "Hypertrophie" => "🔥 Prépare muscles & articulations",
            "Endurance" => "💨 Échauffe avant séries longues",
            _ => "▶️ Top départ"
        };

        return new List<string>
        {
            tag,
            $" 40% ×8 → {Kg(w40)} · ⏱️ 90s · RPE ~6",
            $" 60% ×5 → {Kg(w60)} · ⏱️ 2min",
            $" 75% ×3 → {Kg(w75)} · ⏱️ 2–3min",
            "✅ Même technique · pas d'échec ici"
        };
    }

    private List<string> BuildCoachWork(double totalWeight, string goal)
    {
        if (goal == "Force")
        {
            return new List<string>
            {
                "💪 5 × 5 reps",
                $"⚖️ {Kg(totalWeight)} fixe",
                "⏱️ 3 min / série",
                "🎯 RPE 8–9 dès la série 3",
                "📈 +2,5 kg si 5×5 clean",
                "🛡️ Rack · pinces · spotter si bench lourd"
            };
        }

        if (goal == "Hypertrophie")
        {
            double lo = Math.Round(totalWeight * 0.7);
            double hi = Math.Round(totalWeight * 0.8);
            return new List<string>
            {
                "💪 4 × (8–12) reps",
                $"⚖️ {Kg(lo)} – {Kg(hi)}",
                "⏱️ 60–90s / série",
                "🔥 RIR 1–2",
                "⬇️ Excentrique 2–3s",
                "➕ Dernière série : dropset OK"
            };
        }

        if (goal == "Endurance")
        {
            double lo = Math.Round(totalWeight * 0.5);
            double hi = Math.Round(totalWeight * 0.6);
            return new List<string>
            {
                "💪 3 × (15–20) reps",
                $"⚖️ {Kg(lo)} – {Kg(hi)}",
                "⏱️ 30–60s / série",
                "📏 ROM full > vitesse",
                "🫁 Expire concentrique",
                "📈 + reps puis + kg"
            };
        }

        return new List<string> { "🎯 Choisis un objectif" };
    }

    private List<string> BuildCoachAdvice(string goal)
    {
        if (goal == "Force")
        {
            return new List<string>
            {
                "😴 7–9h sommeil",
                "💧 Hydrate avant",
                "📝 Note charge × reps",
                "🧠 Tech > ego"
            };
        }

        if (goal == "Hypertrophie")
        {
            return new List<string>
            {
                "🥩 ~2 g prot / kg / j",
                "🔁 Même exo 3–4 sem",
                "⚠️ Brûlure OK · articulation aiguë = stop",
                "📉 Cassé ? −volume"
            };
        }

        if (goal == "Endurance")
        {
            return new List<string>
            {
                "🫀 Essoufflé ? +10–15s repos",
                "🐢 +1–2 reps avant +kg",
                "🔗 Superset si forme OK",
                "⛔ Pas demi-reps"
            };
        }

        return new List<string> { "🎯 Objectif ?" };
    }

    public class CoachItem
    {
        public string Title { get; set; }
        public List<string> Lines { get; set; }
    }

    public class WorkoutSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public double Weight { get; set; }
        public double BarWeight { get; set; } // kg — vaut 0 sur les vieilles séances sans ce champ
        public string Goal { get; set; }
        public DateTime Date { get; set; }

        [JsonIgnore]
        public string DetailLine
        {
            get
            {
                string protocol = Goal switch
                {
                    "Force" => "5×5",
                    "Hypertrophie" => "4×8-12",
                    "Endurance" => "3×15-20",
                    _ => ""
                };

                string icon = Goal switch
                {
                    "Force" => "💪",
                    "Hypertrophie" => "🔥",
                    "Endurance" => "💨",
                    _ => "📋"
                };

                var bits = new List<string>();
                if (!string.IsNullOrEmpty(protocol))
                    bits.Add(protocol);
                if (BarWeight > 0.001)
                    bits.Add($"barre {FormatKgCompact(BarWeight)}");

                string core = bits.Count > 0 ? string.Join(" · ", bits) : Goal ?? "";
                return string.IsNullOrEmpty(core) ? icon : $"{icon} {core}";
            }
        }

        private static string FormatKgCompact(double kg)
        {
            double r = Math.Round(kg * 2) / 2d;
            return r % 1 < 0.001 ? $"{r:0} kg" : $"{r:0.#} kg";
        }
    }

    private void SaveSession(double weight, string goal, double barWeight)
    {
        var session = new WorkoutSession
        {
            Weight = weight,
            BarWeight = barWeight,
            Goal = goal,
            Date = DateTime.Now
        };

        // Historique stocké en JSON sous la clé "History" (chaîne vide = première séance)
        var existingData = Preferences.Get("History", "");
        List<WorkoutSession> sessions;

        if (string.IsNullOrEmpty(existingData))
            sessions = new List<WorkoutSession>();
        else
            sessions = JsonSerializer.Deserialize<List<WorkoutSession>>(existingData);

        sessions.Add(session);

        string json = JsonSerializer.Serialize(sessions);
        Preferences.Set("History", json);
    }

    private List<WorkoutSession> LoadSessions()
    {
        var data = Preferences.Get("History", "");

        if (string.IsNullOrEmpty(data))
            return new List<WorkoutSession>();

        return JsonSerializer.Deserialize<List<WorkoutSession>>(data);
    }

    private void LoadHistory()
    {
        var sessions = LoadSessions()
            .OrderByDescending(s => s.Date)
            .ToList();

        HistoryView.ItemsSource = sessions;
    }

    private void OnAdd2_5(object sender, EventArgs e)
    {
        UpdateWeight(2.5);
    }

    private void OnAdd5(object sender, EventArgs e)
    {
        UpdateWeight(5);
    }

    private void OnRemove2_5(object sender, EventArgs e)
    {
        UpdateWeight(-2.5);
    }

    private void UpdateWeight(double change)
    {
        if (
            !double.TryParse(TotalWeightEntry.Text, out double current))
            current = 0; // parse raté → on part de 0

        current += change;

        if (current < 0)
            current = 0;

        TotalWeightEntry.Text = current.ToString(); 
        OnCalculateClicked(null, null);
    }

    private async void OnDeleteSession(object sender, EventArgs e)
    {
        var swipeItem = sender as SwipeItem;
        var sessionToDelete = swipeItem.CommandParameter as WorkoutSession;
        var sessions = LoadSessions();

        bool confirm = await DisplayAlert("Supprimer", "Tu veux supprimer cette séance ?", "Oui", "Non");

        if (!confirm) return;

        sessions.RemoveAll(s => s.Id == sessionToDelete.Id);

        string json = JsonSerializer.Serialize(sessions);
        Preferences.Set("History", json);


        LoadHistory();
    }

    public class Exercise
    {
        public string Name { get; set; }
        public string Image { get; set; }
    }

    

    // Nom du fichier image selon l'objectif
    private string GetExerciseImage(string goal)
    {
        if (goal == "Force")
            return "force.png";

        if (goal == "Hypertrophie")
            return "hypertrophie.png";

        if (goal == "Endurance")
            return "endurance.png";

        return "";
    }

    private void OnStartTimer(object sender, EventArgs e)
    {
        if (isRunning && !isPaused) return;

        isRunning = true;
        isPaused = false;

        startTime = DateTime.Now - elapsed;

        if (timer == null)
        {
            timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(1);

            timer.Tick += (s, args) =>
            {
                elapsed = DateTime.Now - startTime;
                TimerLabel.Text = elapsed.ToString(@"hh\:mm\:ss");
            };
        }

        timer.Start();
    }

    private void OnPauseTimer(object sender, EventArgs e)
    {
        // Rien à faire si pas lancé ou déjà en pause
        if (!isRunning || isPaused) return;

        isPaused = true;
        timer?.Stop();
    }

    private void OnStopTimer(object sender, EventArgs e)
    {
        isRunning = false;
        isPaused = false;

        timer?.Stop();
        elapsed = TimeSpan.Zero;

        TimerLabel.Text = "00:00:00";
    }

    private void OnResetTimer(object sender, EventArgs e)
    {
        timer?.Stop();
        isRunning = false;
        elapsed = TimeSpan.Zero;
        TimerLabel.Text = "00:00:00";
    }

    private List<Exercise> GetExercises(string goal)
    {
        if (goal == "Force")
        {
            return new List<Exercise>
        {
            new Exercise { Name = "Bench Press", Image = "bench.jpg" },
            new Exercise { Name = "Squat", Image = "squat.jpg" },
            new Exercise { Name = "Deadlift", Image = "deadlift.jpg" },
            new Exercise { Name = "Overhead Press", Image = "ohp.jpg" }
        };
        }

        if (goal == "Hypertrophie")
        {
            return new List<Exercise>
        {
            new Exercise { Name = "Incline Press", Image = "incline.jpg" },
            new Exercise { Name = "Cable Fly", Image = "cablefly.jpg" },
            new Exercise { Name = "Lateral Raise", Image = "lateralraise.jpg" },
            new Exercise { Name = "Lat Pulldown", Image = "latpulldown.jpg" }
        };
        }

        return new List<Exercise>
    {
        new Exercise { Name = "Push-Ups", Image = "pushup.jpg" },
        new Exercise { Name = "Burpees", Image = "burpees.jpg" },
        new Exercise { Name = "Jump Squats", Image = "jumpsquat.jpg" },
        new Exercise { Name = "Mountain Climbers", Image = "climbers.jpg" }
    };
    }
}
