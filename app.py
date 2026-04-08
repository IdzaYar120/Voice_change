import os
import time
import threading
import customtkinter as ctk
from tkinter import filedialog
import torch
from rvc_python.infer import RVCInference

# Налаштування вигляду інтерфейсу
ctk.set_appearance_mode("System")
ctk.set_default_color_theme("blue")

class VoiceChangerApp(ctk.CTk):
    def __init__(self):
        super().__init__()

        self.title("AI Voice Changer (RVC)")
        self.geometry("650x600")
        self.resizable(False, False)

        # Стан програми
        self.device = "cuda:0" if torch.cuda.is_available() else "cpu"
        self.rvc = None
        self.model_loaded = False
        self.is_realtime_running = False

        self.ensure_folders_exist()
        self.setup_ui()
        
        # Запуск ініціалізації у фоні
        threading.Thread(target=self.init_rvc, daemon=True).start()

    def ensure_folders_exist(self):
        """Створює необхідні папки при першому запуску."""
        for folder in ['input', 'output', 'models']:
            os.makedirs(folder, exist_ok=True)

    def init_rvc(self):
        self.log_message(f"Ініціалізація нейромережі на пристрої: {self.device.upper()}...")
        try:
            self.rvc = RVCInference(device=self.device)
            self.log_message("Система готова. Будь ласка, оберіть модель голосу (.pth).")
        except Exception as e:
            self.log_message(f"Помилка ініціалізації: {e}")

    def setup_ui(self):
        # Заголовок
        self.label_title = ctk.CTkLabel(self, text="Нейромережевий Змінювач Голосу", font=ctk.CTkFont(size=22, weight="bold"))
        self.label_title.pack(pady=(20, 10))

        # Блок вибору моделі
        self.frame_model = ctk.CTkFrame(self)
        self.frame_model.pack(pady=10, padx=20, fill="x")
        
        self.btn_load_model = ctk.CTkButton(self.frame_model, text="📁 Обрати модель (.pth)", command=self.load_model)
        self.btn_load_model.pack(side="left", padx=10, pady=10)
        
        self.label_model_name = ctk.CTkLabel(self.frame_model, text="Модель не обрана", text_color="gray")
        self.label_model_name.pack(side="left", padx=10)

        # Блок налаштувань (Pitch)
        self.frame_settings = ctk.CTkFrame(self)
        self.frame_settings.pack(pady=10, padx=20, fill="x")
        
        self.label_pitch = ctk.CTkLabel(self.frame_settings, text="Зміна тональності (Pitch): 0")
        self.label_pitch.pack(pady=(10, 0))
        
        self.slider_pitch = ctk.CTkSlider(self.frame_settings, from_=-12, to=12, number_of_steps=24, command=self.update_pitch_label)
        self.slider_pitch.set(0)
        self.slider_pitch.pack(pady=(0, 10), padx=20, fill="x")

        # Вкладки (Файл / Мікрофон)
        self.tabview = ctk.CTkTabview(self, height=120)
        self.tabview.pack(pady=10, padx=20, fill="x")
        
        self.tab_file = self.tabview.add("Обробка файлу")
        self.tab_rt = self.tabview.add("Реальний час")

        # Кнопки у вкладках
        self.btn_select_file = ctk.CTkButton(self.tab_file, text="🎵 Обрати аудіо та обробити", command=self.process_file_dialog)
        self.btn_select_file.pack(pady=20)

        self.btn_toggle_rt = ctk.CTkButton(self.tab_rt, text="🎤 Увімкнути мікрофон (Тест)", fg_color="#28a745", hover_color="#218838", command=self.toggle_realtime)
        self.btn_toggle_rt.pack(pady=20)

        # Консоль логів
        self.textbox_log = ctk.CTkTextbox(self, height=150, state="disabled", font=ctk.CTkFont(family="Consolas", size=12))
        self.textbox_log.pack(pady=10, padx=20, fill="both", expand=True)

    def update_pitch_label(self, value):
        self.label_pitch.configure(text=f"Зміна тональності (Pitch): {int(value)}")

    # Потокобезпечне логування для Tkinter
    def log_message(self, text):
        self.after(0, self._insert_log, text)

    def _insert_log(self, text):
        self.textbox_log.configure(state="normal")
        self.textbox_log.insert("end", f"> {text}\n")
        self.textbox_log.see("end")
        self.textbox_log.configure(state="disabled")

    def load_model(self):
        if not self.rvc:
            self.log_message("Зачекайте, система ще завантажується...")
            return

        file_path = filedialog.askopenfilename(initialdir="models", filetypes=[("PyTorch Models", "*.pth")])
        if file_path:
            filename = os.path.basename(file_path)
            self.label_model_name.configure(text=filename, text_color="white")
            self.log_message(f"Завантаження моделі: {filename}...")
            
            threading.Thread(target=self._load_model_thread, args=(file_path,), daemon=True).start()

    def _load_model_thread(self, file_path):
        try:
            self.rvc.load_model(file_path)
            
            # Автоматичний пошук індексу
            index_path = file_path.replace(".pth", ".index")
            if os.path.exists(index_path):
                self.rvc.load_index(index_path)
                self.log_message("Модель та індекс успішно завантажені!")
            else:
                self.log_message("Модель завантажена. (Індексний файл не знайдено, якість може бути трохи нижчою).")
                
            self.model_loaded = True
        except Exception as e:
            self.log_message(f"Помилка завантаження моделі: {e}")

    def process_file_dialog(self):
        if not self.model_loaded:
            self.log_message("Помилка: Спочатку оберіть модель голосу!")
            return

        input_file = filedialog.askopenfilename(initialdir="input", filetypes=[("Audio Files", "*.wav *.mp3 *.ogg")])
        if input_file:
            output_file = f"output/result_{int(time.time())}.wav"
            pitch = int(self.slider_pitch.get())
            
            self.log_message(f"Початок обробки... Файл: {os.path.basename(input_file)} | Pitch: {pitch}")
            self.btn_select_file.configure(state="disabled", text="⏳ Обробка...")
            
            threading.Thread(target=self._process_file_thread, args=(input_file, output_file, pitch), daemon=True).start()

    def _process_file_thread(self, input_file, output_file, pitch):
        try:
            self.rvc.infer_file(
                input_path=input_file,
                output_path=output_file,
                pitch_up_key=pitch,
                method="rmvpe",      # Найкращий алгоритм аналізу висоти
                index_rate=0.75,     
                filter_radius=3,
                resample_sr=0,
                rms_mix_rate=0.25
            )
            self.log_message(f"✅ Успіх! Файл збережено: {output_file}")
        except Exception as e:
            self.log_message(f"❌ Помилка обробки: {e}")
        finally:
            self.after(0, lambda: self.btn_select_file.configure(state="normal", text="🎵 Обрати аудіо та обробити"))

    def toggle_realtime(self):
        if not self.model_loaded:
            self.log_message("Помилка: Спочатку оберіть модель голосу!")
            return

        if not self.is_realtime_running:
            self.is_realtime_running = True
            self.btn_toggle_rt.configure(text="⏹ Зупинити мікрофон", fg_color="#dc3545", hover_color="#c82333")
            self.log_message("Запуск потокового аудіо... (Режим бета-тестування)")
            threading.Thread(target=self._realtime_audio_loop, daemon=True).start()
        else:
            self.is_realtime_running = False
            self.btn_toggle_rt.configure(text="🎤 Увімкнути мікрофон (Тест)", fg_color="#28a745", hover_color="#218838")
            self.log_message("Мікрофон зупинено.")

    def _realtime_audio_loop(self):
        """Модуль реального часу."""
        import sounddevice as sd
        import numpy as np

        fs = 44100
        duration = 1.0  # Буфер 1 секунда
        
        self.log_message("⚠️ Для повноцінного RVC у реальному часі без затримок краще використовувати спеціалізовані клієнти (напр. W-Okada).")
        
        while self.is_realtime_running:
            time.sleep(1)
            if self.is_realtime_running:
                pass # Тут має бути логіка захоплення буфера sd.rec і передачі в модель.
                # Бібліотека rvc-python наразі краще оптимізована під файли.

if __name__ == "__main__":
    app = VoiceChangerApp()
    app.mainloop()