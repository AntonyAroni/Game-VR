# Guía de Instalación y Puesta a Punto en Arch Linux
**Proyecto:** Zombie Checkpoint VR  
**Repositorio:** `https://github.com/AntonyAroni/Game-VR.git`  
**Rama principal:** `main` (Último commit: `ddb9dd5`)  
**Motor:** Unity 6 (`6000.6.0f1`) + OpenXR + XR Hands + Meta Quest  

---

## 1. Confirmación del Estado en Git

> [!IMPORTANT]
> **El 100% de los cambios, soluciones y depuraciones están subidos al repositorio remoto (`origin/main`).**  
> Tu árbol de trabajo local está completamente limpio. Puedes formatear e instalar tu nuevo sistema operativo con total tranquilidad.

---

## 2. Paquetes Base del Sistema (Arch Linux)

Tras instalar Arch Linux, abre una terminal y actualiza el sistema:

```bash
sudo pacman -Syu
```

### A. Herramientas de Desarrollo y Android
Instala las herramientas de compilación, control de versiones y utilidades para Android / Meta Quest:

```bash
sudo pacman -S --needed base-devel git git-lfs android-tools android-udev
```

* `android-tools`: Provee `adb` y `fastboot`.
* `android-udev`: Reglas de udev actualizadas que reconocen automáticamente el visor Meta Quest (`Vendor ID: 2833`) sin necesidad de configuraciones manuales.

### B. Drivers Gráficos y Vulkan
Asegúrate de tener soporte de Vulkan y aceleración gráfica según tu tarjeta:

* **Si tienes NVIDIA:**
  ```bash
  sudo pacman -S nvidia nvidia-utils lib32-nvidia-utils vulkan-icd-loader lib32-vulkan-icd-loader
  ```
* **Si tienes AMD:**
  ```bash
  sudo pacman -S mesa lib32-mesa vulkan-radeon lib32-vulkan-radeon xf86-video-amdgpu
  ```

---

## 3. Configuración de Permisos USB para Meta Quest

Para que Unity y ADB se comuniquen con las gafas sin pedir permisos `root`:

1. Agrega tu usuario al grupo `adbusers`:
   ```bash
   sudo usermod -aG adbusers $USER
   ```
2. Recarga las reglas udev del sistema:
   ```bash
   sudo udevadm control --reload-rules
   sudo udevadm trigger
   ```
3. *(Opcional/Recomendado)* Reinicia la sesión o la PC para que el grupo `adbusers` tome efecto.

---

## 4. Instalación de Unity Hub

Unity Hub está disponible en el repositorio de usuarios de Arch (AUR). Si usas un helper como `yay` o `paru`:

```bash
yay -S unityhub
```

*(Si aún no tienes `yay`, instálalo rápidamente con:)*
```bash
git clone https://aur.archlinux.org/yay.git
cd yay && makepkg -si && cd .. && rm -rf yay
```

---

## 5. Instalación de Unity Editor y Módulos Android

1. Abre **Unity Hub** e inicia sesión con tu cuenta de Unity.
2. Ve a la pestaña **Installs** y presiona **Install Editor**.
3. Selecciona la versión del proyecto:
   * Versión exacta: **Unity 6 (6000.6.0f1)**.
   * *Enlace directo de instalación rápida en el navegador:*  
     `unityhub://6000.6.0f1/f7f8ed4d1e24`
4. **Al instalar, marca obligatoriamente los siguientes módulos:**
   * [x] **Android Build Support**
   * [x] **Android SDK & NDK Tools** *(Fundamental: Unity configurará las versiones exactas internamente)*
   * [x] **OpenJDK**
   * [x] **Linux Build Support (Mono)** *(Para compilar/probar en PC si fuera necesario)*

> [!TIP]
> Al dejar que Unity Hub instale el **SDK, NDK y OpenJDK integrados**, no tendrás que configurar variables de entorno `ANDROID_HOME` ni resolver incompatibilidades de versiones de NDK en Linux.

---

## 6. Clonar el Proyecto

Elige tu directorio de trabajo (por ejemplo `~/Documents/IHC` o `~/Proyectos`):

```bash
mkdir -p ~/Documents/IHC
cd ~/Documents/IHC
git clone https://github.com/AntonyAroni/Game-VR.git
```

En **Unity Hub**:
1. Haz clic en **Add** (o *Add project from disk*).
2. Selecciona la carpeta `Game-VR`.
3. Abre el proyecto con Unity `6000.6.0f1`. La primera apertura tardará unos minutos mientras compila los shaders de URP y reindexa el Asset Database.

---

## 7. Conectar y Autorizar las Meta Quest

1. **Modo Desarrollador:** Verifica que el modo de desarrollador siga activo en las gafas a través de la app móvil **Meta Horizon** (*Configuración de dispositivos > Modo de desarrollador*).
2. **Conectar por USB:** Conecta las gafas al PC mediante un cable USB-C con soporte de datos (preferiblemente USB 3.0).
3. **Autorización RSA:**
   * Colócate el visor.
   * Aparecerá una ventana: *"¿Permitir depuración USB?"*
   * Marca la casilla: **"Permitir siempre desde esta computadora"** y selecciona **Permitir**.
4. **Verificar en terminal:**
   ```bash
   adb devices
   ```
   Debe responder con el número de serie de tu visor y la palabra `device`:
   ```text
   List of devices attached
   1WMHH831Z01031    device
   ```
   *(Si dice `unauthorized`, colócate el visor y pulsa "Permitir"; si dice `no permissions`, revisa el paso de `adbusers` y udev).*

---

## 8. Compilar y Probar en las Gafas

En el Editor de Unity, el proyecto incluye un asistente automatizado para Meta Quest:

### Método Rápido (Recomendado):
1. En la barra superior de Unity, haz clic en el menú:
   * **`Zombie Checkpoint` ▸ `1. Configure Quest Settings`** (configura automáticamente Android, ARM64, IL2CPP, Floor Tracking y escenas de build).
   * **`Zombie Checkpoint` ▸ `2. Build and Run to Quest 2`** (compila el APK en `Builds/ZombieCheckpoint.apk`, lo transfiere vía ADB y lo inicia inmediatamente en las gafas).

### Método Tradicional (Build Settings):
1. Ve a **File ▸ Build Settings**.
2. Asegúrate de que la plataforma activa sea **Android** (si no, pulsa *Switch Platform*).
3. En **Run Device**, selecciona tus gafas (`Oculus Quest ...`).
4. Haz clic en **Build and Run**.

---

## 9. Comandos Útiles de Depuración en Arch Linux

* **Ver registros del juego en tiempo real (Logcat):**
  ```bash
  adb logcat -s Unity
  ```
* **Instalar un APK manualmente si lo compilaste aparte:**
  ```bash
  adb install -r -d Builds/ZombieCheckpoint.apk
  ```
* **Desinstalar la versión anterior si hay conflicto de firmas:**
  ```bash
  adb uninstall com.DefaultCompany.GameVR
  ```
* **Reiniciar el demonio de ADB:**
  ```bash
  adb kill-server && adb start-server
  ```
