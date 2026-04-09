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
        if torch.cuda.is_available():
            self.device = "cuda:0"
        elif hasattr(torch.backends, 'mps') and torch.backends.mps.is_available():
            self.device = "mps"
        else:
            self.device = "cpu"
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

        # Блок налаштувань (Pitch, Index Rate, Method)
        self.frame_settings = ctk.CTkFrame(self)
        self.frame_settings.pack(pady=10, padx=20, fill="x")
        
        # Рядок з підписами
        self.label_pitch = ctk.CTkLabel(self.frame_settings, text="Тональність: 0")
        self.label_pitch.grid(row=0, column=0, padx=10, pady=(10, 0), sticky="w")
        
        self.label_index = ctk.CTkLabel(self.frame_settings, text="Index Rate: 0.75")
        self.label_index.grid(row=0, column=1, padx=10, pady=(10, 0), sticky="w")
        
        self.label_method = ctk.CTkLabel(self.frame_settings, text="Алгоритм:")
        self.label_method.grid(row=0, column=2, padx=10, pady=(10, 0), sticky="w")

        # Рядок з елементами керування
        self.slider_pitch = ctk.CTkSlider(self.frame_settings, from_=-12, to=12, number_of_steps=24, command=self.update_pitch_label)
        self.slider_pitch.set(0)
        self.slider_pitch.grid(row=1, column=0, padx=10, pady=(0, 10), sticky="ew")

        self.slider_index = ctk.CTkSlider(self.frame_settings, from_=0.0, to=1.0, number_of_steps=20, command=self.update_index_label)
        self.slider_index.set(0.75)
        self.slider_index.grid(row=1, column=1, padx=10, pady=(0, 10), sticky="ew")

        self.combo_method = ctk.CTkOptionMenu(self.frame_settings, values=["rmvpe", "pm", "harvest"])
        self.combo_method.set("rmvpe")
        self.combo_method.grid(row=1, column=2, padx=10, pady=(0, 10), sticky="ew")

        self.frame_settings.grid_columnconfigure((0, 1, 2), weight=1)

        # Контейнер для кнопки обробки (замість Вкладок)
        self.frame_action = ctk.CTkFrame(self)
        self.frame_action.pack(pady=10, padx=20, fill="x")

        self.btn_select_file = ctk.CTkButton(self.frame_action, text="🎵 Обрати аудіо та обробити", height=40, font=ctk.CTkFont(size=14, weight="bold"), command=self.process_file_dialog)
        self.btn_select_file.pack(pady=20)

        # Консоль логів
        self.textbox_log = ctk.CTkTextbox(self, height=150, state="disabled", font=ctk.CTkFont(family="Consolas", size=12))
        self.textbox_log.pack(pady=10, padx=20, fill="both", expand=True)

    def update_pitch_label(self, value):
        self.label_pitch.configure(text=f"Тональність: {int(value)}")

    def update_index_label(self, value):
        self.label_index.configure(text=f"Index Rate: {value:.2f}")

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
            index_path = os.path.splitext(file_path)[0] + ".index"
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
            index_rate = float(self.slider_index.get())
            method = self.combo_method.get()
            
            self.log_message(f"Початок обробки... Файл: {os.path.basename(input_file)} | Pitch: {pitch} | Method: {method}")
            self.btn_select_file.configure(state="disabled", text="⏳ Обробка...")
            
            threading.Thread(target=self._process_file_thread, args=(input_file, output_file, pitch, index_rate, method), daemon=True).start()

    def _process_file_thread(self, input_file, output_file, pitch, index_rate, method):
        try:
            self.rvc.infer_file(
                input_path=input_file,
                output_path=output_file,
                pitch_up_key=pitch,
                method=method,
                index_rate=index_rate,     
                filter_radius=3,
                resample_sr=0,
                rms_mix_rate=0.25
            )
            self.log_message(f"✅ Успіх! Файл збережено: {output_file}")
            
            # Спробувати відкрити папку з результатом
            if hasattr(os, 'startfile'):
                os.startfile(os.path.dirname(os.path.abspath(output_file)))
        except Exception as e:
            self.log_message(f"❌ Помилка обробки: {e}")
        finally:
            self.after(0, lambda: self.btn_select_file.configure(state="normal", text="🎵 Обрати аудіо та обробити"))



if __name__ == "__main__":
    app = VoiceChangerApp()
    app.mainloop()