---@meta

---
--- Lilly.Engine vVersionInfoData { AppName = Lilly.Engine, CodeName = Oceanus, Version = 0.5.0.0 } Lua API
--- Auto-generated on 2025-12-03 12:48:14
---

--- Global constants

---
--- VERSION constant
--- Value: "0.0.1"
---
---@type string
VERSION = "0.0.1"

---
--- ENGINE_VERSION constant
--- Value: "VersionInfoData { AppName = Lilly.Engine, CodeName = Oceanus, Version = 0.5.0.0 }"
---
---@type string
ENGINE_VERSION = "VersionInfoData { AppName = Lilly.Engine, CodeName = Oceanus, Version = 0.5.0.0 }"

---
--- ENGINE constant
--- Value: "SquidVox"
---
---@type string
ENGINE = "SquidVox"

---
--- PLATFORM constant
--- Value: "Unix"
---
---@type string
PLATFORM = "Unix"


---
--- ConsoleModule module
---
--- Console API for logging and debugging
---
---@class console
console.assert = function() end
console.clear = function() end
console.debug = function() end
console.error = function() end
console.info = function() end
console.log = function() end
console.trace = function() end
console.warn = function() end
console = {}

---
--- No description available
---
---@param condition boolean The condition flag
---@param ... any The arguments of type any
function console.assert(condition, ...) end

---
--- No description available
---
function console.clear() end

---
--- No description available
---
---@param ... any The arguments of type any
function console.debug(...) end

---
--- No description available
---
---@param ... any The arguments of type any
function console.error(...) end

---
--- No description available
---
---@param ... any The arguments of type any
function console.info(...) end

---
--- No description available
---
---@param ... any The arguments of type any
function console.log(...) end

---
--- No description available
---
---@param ... any The arguments of type any
function console.trace(...) end

---
--- No description available
---
---@param ... any The arguments of type any
function console.warn(...) end

---
--- AssetsModule module
---
--- Provides functions to interact with game assets.
---
---@class assets
assets.load_font = function() end
assets.load_texture = function() end
assets.load_atlas = function() end
assets = {}

---
--- Loads a font from the specified file path and returns its name.
---
---@param font_name string The fontname text
---@param font_path string The fontpath text
---@return string The resulting text
function assets.load_font(font_name, font_path) end

---
--- Loads a texture from the specified file path and returns its name.
---
---@param texture_name string The texturename text
---@param texture_path string The texturepath text
---@return string The resulting text
function assets.load_texture(texture_name, texture_path) end

---
--- Loads a texture atlas from the specified file path and returns its name.
---
---@param atlas_name string The atlasname text
---@param atlas_path string The atlaspath text
---@param tile_width number The tilewidth value
---@param tile_height number The tileheight value
---@param margin number The margin value (optional)
---@param spacing number The spacing value (optional)
---@return string The resulting text
function assets.load_atlas(atlas_name, atlas_path, tile_width, tile_height, margin, spacing) end

---
--- EngineModule module
---
--- Provides core engine functionalities.
---
---@class engine
engine.on_update = function() end
engine = {}

---
--- Registers a closure to be called on each engine update cycle.
---
---@param update function The update of type function
function engine.on_update(update) end

---
--- NotificationsModule module
---
--- Notification utilities and functions
---
---@class notifications
notifications.error = function() end
notifications.info = function() end
notifications.success = function() end
notifications.test_all = function() end
notifications.test_script_error = function() end
notifications.warning = function() end
notifications = {}

---
--- Raises an error notification with the specified message.
---
---@param message string The message text
function notifications.error(message) end

---
--- Raises an informational notification with the specified message.
---
---@param message string The message text
function notifications.info(message) end

---
--- Raises a success notification with the specified message.
---
---@param message string The message text
function notifications.success(message) end

---
--- Shows all notification types for testing purposes.
---
function notifications.test_all() end

---
--- Triggers a Lua script error to test the error display system.
---
function notifications.test_script_error() end

---
--- Raises a warning notification with the specified message.
---
---@param message string The message text
function notifications.warning(message) end

---
--- JobSystemModule module
---
---@class job_system
job_system.run_in_main_thread = function() end
job_system.schedule = function() end
job_system = {}

---
--- Schedules a closure to be executed in the main thread.
---
---@param closure function The closure of type function
function job_system.run_in_main_thread(closure) end

---
--- Schedules a job to be executed by the job system.
---
---@param name string The name text
---@param closure function The closure of type function
---@param user_data any The userdata of type any (optional)
function job_system.schedule(name, closure, user_data) end

---
--- InputManagerModule module
---
--- Input Manager Module
---
---@class input_manager
input_manager.bind_key = function() end
input_manager.bind_key_held = function() end
input_manager.bind_key_repeat = function() end
input_manager.bind_key_context = function() end
input_manager.bind_mouse_click = function() end
input_manager.bind_mouse_click_context = function() end
input_manager.bind_mouse = function() end
input_manager.bind_mouse_context = function() end
input_manager.clear_bindings = function() end
input_manager.convert_mouse_delta_to_pitch_yaw_roll = function() end
input_manager.get_context = function() end
input_manager.get_mouse_delta = function() end
input_manager.grab_mouse = function() end
input_manager.release_mouse = function() end
input_manager.is_key_down = function() end
input_manager.is_key_pressed = function() end
input_manager.is_mouse_visible = function() end
input_manager.set_context = function() end
input_manager.show_mouse = function() end
input_manager.toggle_mouse = function() end
input_manager.test_key_name = function() end
input_manager.unbind_key = function() end
input_manager.unbind_key_held = function() end
input_manager.unbind_key_repeat = function() end
input_manager = {}

---
--- Binds a key to a callback action.
---
---@param key_binding string The keybinding text
---@param callback function The callback of type function
function input_manager.bind_key(key_binding, callback) end

---
--- Binds a key to a callback that executes every frame while held.
---
---@param key_binding string The keybinding text
---@param callback function The callback of type function
function input_manager.bind_key_held(key_binding, callback) end

---
--- Binds a key to a callback with key repeat (delay + interval).
---
---@param key_binding string The keybinding text
---@param callback function The callback of type function
function input_manager.bind_key_repeat(key_binding, callback) end

---
--- Binds a key to a callback action with a specific context.
---
---@param key_binding string The keybinding text
---@param callback function The callback of type function
---@param context_name string The contextname text
function input_manager.bind_key_context(key_binding, callback, context_name) end

---
--- Binds a callback to global mouse click events (always active).
---
---@param callback function The callback of type function
function input_manager.bind_mouse_click(callback) end

---
--- Binds a callback to mouse click events for a specific context.
---
---@param callback function The callback of type function
---@param context_name string The contextname text
function input_manager.bind_mouse_click_context(callback, context_name) end

---
--- Binds a callback to global mouse movement (always active).
---
---@param callback function The callback of type function
function input_manager.bind_mouse(callback) end

---
--- Binds a callback to mouse movement for a specific context.
---
---@param callback function The callback of type function
---@param context_name string The contextname text
function input_manager.bind_mouse_context(callback, context_name) end

---
--- Clears all key bindings.
---
function input_manager.clear_bindings() end

---
--- Converts mouse delta to pitch, yaw, and roll values.
---
---@param delta_x number The deltax value
---@param delta_y number The deltay value
---@param roll number The roll value (optional)
---@param sensitivity number The sensitivity value (optional)
---@return Table The result as Table
function input_manager.convert_mouse_delta_to_pitch_yaw_roll(delta_x, delta_y, roll, sensitivity) end

---
--- Gets the current input context.
---
---@return string The resulting text
function input_manager.get_context() end

---
--- Gets the mouse delta (movement since last frame).
---
---@return DynValue The result as DynValue
function input_manager.get_mouse_delta() end

---
--- Grabs the mouse cursor and makes it invisible.
---
function input_manager.grab_mouse() end

---
--- Releases the mouse cursor and makes it visible.
---
function input_manager.release_mouse() end

---
--- Checks if a key is currently down.
---
---@param key_name string The keyname text
---@return boolean The result of the operation
function input_manager.is_key_down(key_name) end

---
--- Checks if a key was just pressed.
---
---@param key_name string The keyname text
---@return boolean The result of the operation
function input_manager.is_key_pressed(key_name) end

---
--- Checks if the mouse cursor is visible.
---
---@return boolean The result of the operation
function input_manager.is_mouse_visible() end

---
--- Sets the current input context.
---
---@param context_name string The contextname text
function input_manager.set_context(context_name) end

---
--- Shows the mouse cursor.
---
function input_manager.show_mouse() end

---
--- Sets the mouse cursor visibility.
---
function input_manager.toggle_mouse() end

---
--- Tests if a key name is valid and returns debug info.
---
---@param key_name string The keyname text
---@return string The resulting text
function input_manager.test_key_name(key_name) end

---
--- Unbinds a key.
---
---@param key_binding string The keybinding text
function input_manager.unbind_key(key_binding) end

---
--- Unbinds a held key.
---
---@param key_binding string The keybinding text
function input_manager.unbind_key_held(key_binding) end

---
--- Unbinds a repeat key.
---
---@param key_binding string The keybinding text
function input_manager.unbind_key_repeat(key_binding) end

---
--- WindowModule module
---
--- Provides functions to interact with the application window.
---
---@class window
window.get_title = function() end
window.set_title = function() end
window.set_vsync = function() end
window.set_refresh_rate = function() end
window = {}

---
--- Gets the title of the application window.
---
---@return string The resulting text
function window.get_title() end

---
--- Sets the title of the application window.
---
---@param title string The title text
function window.set_title(title) end

---
--- Enables or disables vertical synchronization (VSync) for the application window.
---
---@param v_sync boolean The vsync flag
function window.set_vsync(v_sync) end

---
--- Sets the refresh rate of the application window.
---
---@param refresh_rate number The refreshrate value
function window.set_refresh_rate(refresh_rate) end

---
--- CameraModule module
---
--- Provides functionality related to camera management.
---
---@class camera
camera.dispatch_keyboard = function() end
camera.dispatch_keyboard_fps = function() end
camera.dispatch_mouse = function() end
camera.dispatch_mouse_fps = function() end
camera.register_fps = function() end
camera.set_active = function() end
camera = {}

---
--- Dispatches keyboard input to the current camera for movement.
---
---@param forward number The forward value
---@param right number The right value
---@param up number The up value
function camera.dispatch_keyboard(forward, right, up) end

---
--- Dispatches keyboard input for FPS camera movement relative to camera orientation.
---
---@param forward number The forward value
---@param right number The right value
---@param up number The up value
function camera.dispatch_keyboard_fps(forward, right, up) end

---
--- Dispatches mouse movement to the current camera for rotation.
---
---@param yaw number The yaw value
---@param pitch number The pitch value
---@param roll number The roll value
function camera.dispatch_mouse(yaw, pitch, roll) end

---
--- Dispatches mouse delta directly for FPS camera using Look method.
---
---@param pitch_delta number The pitchdelta value
---@param yaw_delta number The yawdelta value
function camera.dispatch_mouse_fps(pitch_delta, yaw_delta) end

---
--- Registers a first-person camera with the given name.
---
---@param name string The name text
function camera.register_fps(name) end

---
--- Sets the active camera by its name.
---
---@param name string The name text
function camera.set_active(name) end

---
--- ScenesModule module
---
--- Provides functionality related to scene management.
---
---@class scenes
scenes.add_scene = function() end
scenes.load_scene = function() end
scenes = {}

---
--- Adds a new scene to the scene manager.
---
---@param name string The name text
---@param load_function function The loadfunction of type function
function scenes.add_scene(name, load_function) end

---
--- Loads a scene by its name.
---
---@param name string The name text
function scenes.load_scene(name) end

---
--- CommandsModule module
---
--- Provides access to the command system.
---
---@class commands
commands.register = function() end
commands = {}

---
--- Registers a new command in the command system.
---
---@param command_name string The commandname text
---@param description string The description text
---@param aliases string The aliases text
---@param execute_function function The executefunction of type function
function commands.register(command_name, description, aliases, execute_function) end

---
--- Rendering3dModule module
---
--- Provides functions to interact with 2D/3D rendering features.
---
---@class rendering
rendering.toggle_wireframe = function() end
rendering.is_wireframe = function() end
rendering = {}

---
--- Toggles the wireframe rendering mode for 3D objects.
---
function rendering.toggle_wireframe() end

---
--- Checks if the wireframe rendering mode is enabled for 3D objects.
---
---@return boolean The result of the operation
function rendering.is_wireframe() end

---
--- ImGuiModule module
---
--- Complete ImGui drawing and interaction library
---
---@class im_gui
im_gui.arrow_button = function() end
im_gui.begin_group = function() end
im_gui.begin_popup = function() end
im_gui.begin_popup_modal = function() end
im_gui.begin_tooltip = function() end
im_gui.bullet_text = function() end
im_gui.button = function() end
im_gui.calc_text_size = function() end
im_gui.checkbox = function() end
im_gui.close_current_popup = function() end
im_gui.collapsing_header = function() end
im_gui.color_picker3 = function() end
im_gui.color_picker4 = function() end
im_gui.dummy = function() end
im_gui.end_group = function() end
im_gui.end_popup = function() end
im_gui.end_tooltip = function() end
im_gui.get_cursor_pos = function() end
im_gui.get_draw_data = function() end
im_gui.get_frame_height = function() end
im_gui.get_frame_height_with_spacing = function() end
im_gui.get_io = function() end
im_gui.get_mouse_pos = function() end
im_gui.get_window_pos = function() end
im_gui.get_window_size = function() end
im_gui.indent = function() end
im_gui.input_float = function() end
im_gui.input_float2 = function() end
im_gui.input_float3 = function() end
im_gui.input_int = function() end
im_gui.input_int2 = function() end
im_gui.input_text = function() end
im_gui.input_text_multiline = function() end
im_gui.invisible_button = function() end
im_gui.is_item_hovered = function() end
im_gui.is_key_down = function() end
im_gui.is_key_pressed = function() end
im_gui.is_mouse_clicked = function() end
im_gui.is_mouse_down = function() end
im_gui.is_popup_open = function() end
im_gui.label_text = function() end
im_gui.new_line = function() end
im_gui.open_popup = function() end
im_gui.pop_style_color = function() end
im_gui.pop_style_var = function() end
im_gui.push_style_color = function() end
im_gui.push_style_var_float = function() end
im_gui.push_style_var_vec2 = function() end
im_gui.radio_button = function() end
im_gui.same_line = function() end
im_gui.separator = function() end
im_gui.set_cursor_pos = function() end
im_gui.set_item_tooltip = function() end
im_gui.set_next_item_open = function() end
im_gui.show_demo_window = function() end
im_gui.show_metrics_window = function() end
im_gui.slider_float = function() end
im_gui.slider_float2 = function() end
im_gui.slider_float3 = function() end
im_gui.slider_int = function() end
im_gui.slider_int2 = function() end
im_gui.small_button = function() end
im_gui.spacing = function() end
im_gui.text = function() end
im_gui.text_colored = function() end
im_gui.text_disabled = function() end
im_gui.text_wrapped = function() end
im_gui.tree_node = function() end
im_gui.tree_node_ex = function() end
im_gui.tree_pop = function() end
im_gui.unindent = function() end
im_gui.vslider_float = function() end
im_gui.vslider_int = function() end
im_gui = {}

---
--- No description available
---
---@param id string The id text
---@param direction number The direction value
---@return boolean The result of the operation
function im_gui.arrow_button(id, direction) end

---
--- No description available
---
function im_gui.begin_group() end

---
--- No description available
---
---@param id string The id text
---@param flags number The flags value (optional)
---@return boolean The result of the operation
function im_gui.begin_popup(id, flags) end

---
--- No description available
---
---@param title string The title text
---@param flags number The flags value (optional)
---@return boolean The result of the operation
function im_gui.begin_popup_modal(title, flags) end

---
--- No description available
---
function im_gui.begin_tooltip() end

---
--- No description available
---
---@param message string The message text
function im_gui.bullet_text(message) end

---
--- No description available
---
---@param label string The label text
---@param width number The width value (optional)
---@param height number The height value (optional)
---@return boolean The result of the operation
function im_gui.button(label, width, height) end

---
--- No description available
---
---@param text string The text text
---@return string The resulting text
function im_gui.calc_text_size(text) end

---
--- No description available
---
---@param label string The label text
---@param value boolean The value flag
---@return boolean The result of the operation
function im_gui.checkbox(label, value) end

---
--- No description available
---
function im_gui.close_current_popup() end

---
--- No description available
---
---@param label string The label text
---@param flags number The flags value (optional)
---@return boolean The result of the operation
function im_gui.collapsing_header(label, flags) end

---
--- No description available
---
---@param label string The label text
---@param r number The r value
---@param g number The g value
---@param b number The b value
---@return string The resulting text
function im_gui.color_picker3(label, r, g, b) end

---
--- No description available
---
---@param label string The label text
---@param r number The r value
---@param g number The g value
---@param b number The b value
---@param a number The a value
---@return string The resulting text
function im_gui.color_picker4(label, r, g, b, a) end

---
--- No description available
---
---@param width number The width value
---@param height number The height value
function im_gui.dummy(width, height) end

---
--- No description available
---
function im_gui.end_group() end

---
--- No description available
---
function im_gui.end_popup() end

---
--- No description available
---
function im_gui.end_tooltip() end

---
--- No description available
---
---@return string The resulting text
function im_gui.get_cursor_pos() end

---
--- No description available
---
---@return string The resulting text
function im_gui.get_draw_data() end

---
--- No description available
---
---@return number The computed numeric value
function im_gui.get_frame_height() end

---
--- No description available
---
---@return number The computed numeric value
function im_gui.get_frame_height_with_spacing() end

---
--- No description available
---
---@return string The resulting text
function im_gui.get_io() end

---
--- No description available
---
---@return string The resulting text
function im_gui.get_mouse_pos() end

---
--- No description available
---
---@return string The resulting text
function im_gui.get_window_pos() end

---
--- No description available
---
---@return string The resulting text
function im_gui.get_window_size() end

---
--- No description available
---
---@param width number The width value (optional)
function im_gui.indent(width) end

---
--- No description available
---
---@param label string The label text
---@param value number The value value
---@param step number The step value (optional)
---@param step_fast number The stepfast value (optional)
---@return number The computed numeric value
function im_gui.input_float(label, value, step, step_fast) end

---
--- No description available
---
---@param label string The label text
---@param x number The x value
---@param y number The y value
---@return string The resulting text
function im_gui.input_float2(label, x, y) end

---
--- No description available
---
---@param label string The label text
---@param x number The x value
---@param y number The y value
---@param z number The z value
---@return string The resulting text
function im_gui.input_float3(label, x, y, z) end

---
--- No description available
---
---@param label string The label text
---@param value number The value value
---@param step number The step value (optional)
---@param step_fast number The stepfast value (optional)
---@return number The computed numeric value
function im_gui.input_int(label, value, step, step_fast) end

---
--- No description available
---
---@param label string The label text
---@param x number The x value
---@param y number The y value
---@return string The resulting text
function im_gui.input_int2(label, x, y) end

---
--- No description available
---
---@param label string The label text
---@param text string The text text
---@param max_length number The maxlength value (optional)
---@return string The resulting text
function im_gui.input_text(label, text, max_length) end

---
--- No description available
---
---@param label string The label text
---@param text string The text text
---@param width number The width value
---@param height number The height value
---@param max_length number The maxlength value (optional)
---@return string The resulting text
function im_gui.input_text_multiline(label, text, width, height, max_length) end

---
--- No description available
---
---@param id string The id text
---@param width number The width value
---@param height number The height value
---@return boolean The result of the operation
function im_gui.invisible_button(id, width, height) end

---
--- No description available
---
---@param flags number The flags value (optional)
---@return boolean The result of the operation
function im_gui.is_item_hovered(flags) end

---
--- No description available
---
---@param key number The key value
---@return boolean The result of the operation
function im_gui.is_key_down(key) end

---
--- No description available
---
---@param key number The key value
---@return boolean The result of the operation
function im_gui.is_key_pressed(key) end

---
--- No description available
---
---@param button number The button value
---@return boolean The result of the operation
function im_gui.is_mouse_clicked(button) end

---
--- No description available
---
---@param button number The button value
---@return boolean The result of the operation
function im_gui.is_mouse_down(button) end

---
--- No description available
---
---@param id string The id text
---@param flags number The flags value (optional)
---@return boolean The result of the operation
function im_gui.is_popup_open(id, flags) end

---
--- No description available
---
---@param label string The label text
---@param message string The message text
function im_gui.label_text(label, message) end

---
--- No description available
---
function im_gui.new_line() end

---
--- No description available
---
---@param id string The id text
---@param flags number The flags value (optional)
function im_gui.open_popup(id, flags) end

---
--- No description available
---
---@param count number The count value (optional)
function im_gui.pop_style_color(count) end

---
--- No description available
---
---@param count number The count value (optional)
function im_gui.pop_style_var(count) end

---
--- No description available
---
---@param idx number The idx value
---@param r number The r value
---@param g number The g value
---@param b number The b value
---@param a number The a value
function im_gui.push_style_color(idx, r, g, b, a) end

---
--- No description available
---
---@param idx number The idx value
---@param val number The val value
function im_gui.push_style_var_float(idx, val) end

---
--- No description available
---
---@param idx number The idx value
---@param x number The x value
---@param y number The y value
function im_gui.push_style_var_vec2(idx, x, y) end

---
--- No description available
---
---@param label string The label text
---@param active number The active value
---@param button_value number The buttonvalue value
---@return boolean The result of the operation
function im_gui.radio_button(label, active, button_value) end

---
--- No description available
---
---@param offset_from_start number The offsetfromstart value (optional)
---@param spacing number The spacing value (optional)
function im_gui.same_line(offset_from_start, spacing) end

---
--- No description available
---
function im_gui.separator() end

---
--- No description available
---
---@param x number The x value
---@param y number The y value
function im_gui.set_cursor_pos(x, y) end

---
--- No description available
---
---@param tooltip string The tooltip text
function im_gui.set_item_tooltip(tooltip) end

---
--- No description available
---
---@param open boolean The open flag
---@param cond number The cond value (optional)
function im_gui.set_next_item_open(open, cond) end

---
--- No description available
---
function im_gui.show_demo_window() end

---
--- No description available
---
function im_gui.show_metrics_window() end

---
--- No description available
---
---@param label string The label text
---@param value number The value value
---@param min number The min value
---@param max number The max value
---@return number The computed numeric value
function im_gui.slider_float(label, value, min, max) end

---
--- No description available
---
---@param label string The label text
---@param x number The x value
---@param y number The y value
---@param min number The min value
---@param max number The max value
---@return string The resulting text
function im_gui.slider_float2(label, x, y, min, max) end

---
--- No description available
---
---@param label string The label text
---@param x number The x value
---@param y number The y value
---@param z number The z value
---@param min number The min value
---@param max number The max value
---@return string The resulting text
function im_gui.slider_float3(label, x, y, z, min, max) end

---
--- No description available
---
---@param label string The label text
---@param value number The value value
---@param min number The min value
---@param max number The max value
---@return number The computed numeric value
function im_gui.slider_int(label, value, min, max) end

---
--- No description available
---
---@param label string The label text
---@param x number The x value
---@param y number The y value
---@param min number The min value
---@param max number The max value
---@return string The resulting text
function im_gui.slider_int2(label, x, y, min, max) end

---
--- No description available
---
---@param label string The label text
---@return boolean The result of the operation
function im_gui.small_button(label) end

---
--- No description available
---
function im_gui.spacing() end

---
--- No description available
---
---@param message string The message text
function im_gui.text(message) end

---
--- No description available
---
---@param r number The r value
---@param g number The g value
---@param b number The b value
---@param a number The a value
---@param message string The message text
function im_gui.text_colored(r, g, b, a, message) end

---
--- No description available
---
---@param message string The message text
function im_gui.text_disabled(message) end

---
--- No description available
---
---@param message string The message text
function im_gui.text_wrapped(message) end

---
--- No description available
---
---@param label string The label text
---@return boolean The result of the operation
function im_gui.tree_node(label) end

---
--- No description available
---
---@param label string The label text
---@param flags number The flags value (optional)
---@return boolean The result of the operation
function im_gui.tree_node_ex(label, flags) end

---
--- No description available
---
function im_gui.tree_pop() end

---
--- No description available
---
---@param width number The width value (optional)
function im_gui.unindent(width) end

---
--- No description available
---
---@param label string The label text
---@param width number The width value
---@param height number The height value
---@param value number The value value
---@param min number The min value
---@param max number The max value
---@return number The computed numeric value
function im_gui.vslider_float(label, width, height, value, min, max) end

---
--- No description available
---
---@param label string The label text
---@param width number The width value
---@param height number The height value
---@param value number The value value
---@param min number The min value
---@param max number The max value
---@return number The computed numeric value
function im_gui.vslider_int(label, width, height, value, min, max) end

---
--- QuakeConsoleModule module
---
--- Provides functionality for the Quake-style console.
---
---@class quake_console
quake_console.toggle = function() end
quake_console = {}

---
--- Toggles the visibility of the console.
---
function quake_console.toggle() end

---
--- BlockRegistryModule module
---
--- Provides access to the block registry, allowing retrieval of block types and their properties.
---
---@class block_registry
block_registry.register_block = function() end
block_registry.new_block = function() end
block_registry.load_blocks_from_data = function() end
block_registry = {}

---
--- Registers a new block type with the given name and properties defined in the builder closure.
---
---@param name string The name text
---@param builder function The builder of type function
function block_registry.register_block(name, builder) end

---
--- Creates a new block type with the specified name.
---
---@param name string The name text
---@return BlockType The result as BlockType
function block_registry.new_block(name) end

---
--- Loads block definitions from a JSON file in the data directory.
---
---@param file_name string The filename text
function block_registry.load_blocks_from_data(file_name) end

---
--- GenerationModule module
---
--- Provides functionalities related to voxel world generation.
---
---@class generation
generation = {}

---
--- WorldModule module
---
--- Provides access to world-related functionalities.
---
---@class world
world.remove_block = function() end
world = {}

---
--- Removes the block at the current targeted position in the voxel world.
---
function world.remove_block() end

---
--- game_objects module 
---
---@class game_objects
game_objects = {}

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_block_outline(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_button(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_camera_debugger(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_chunk(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_chunk_debugger_viewer(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_combo_box(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_crosshair(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_fps(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_job_system_debugger(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_list_box(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_notification_hud(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_performance_debugger(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_quake_console(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_rain_effect(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_rectangle(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_render_pipeline_debugger(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_simple_cube(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_sky(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_snow_effect(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_stack_layout(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_text(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_text_edit(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_texture(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_version(...) end

---
--- Dynamically registered function
---
---@param ... any
---@return any
function game_objects.new_world(...) end


---
--- Enum: Lilly.Voxel.Plugin.Types.BlockRenderType
--- This enum is read-only and case-insensitive
---
---@class block_render_type
---@field public readonly Solid number # Enum value: 0
---@field public readonly Transparent number # Enum value: 1
---@field public readonly Billboard number # Enum value: 2
---@field public readonly Cutout number # Enum value: 3
---@field public readonly Item number # Enum value: 4
---@field public readonly Fluid number # Enum value: 5
---@field public readonly Clouds number # Enum value: 6

--- Read-only enum table
block_render_type = {}


---
--- Enum: Lilly.Voxel.Plugin.Types.BlockFace
--- This enum is read-only and case-insensitive
---
---@class block_face
---@field public readonly Top number # Enum value: 0
---@field public readonly Bottom number # Enum value: 1
---@field public readonly Front number # Enum value: 2
---@field public readonly Back number # Enum value: 3
---@field public readonly Left number # Enum value: 4
---@field public readonly Right number # Enum value: 5

--- Read-only enum table
block_face = {}


---
--- Enum: MoonSharp.Interpreter.DataType
--- This enum is read-only and case-insensitive
---
---@class data_type
---@field public readonly Nil number # Enum value: 0
---@field public readonly Void number # Enum value: 1
---@field public readonly Boolean number # Enum value: 2
---@field public readonly Number number # Enum value: 3
---@field public readonly String number # Enum value: 4
---@field public readonly Function number # Enum value: 5
---@field public readonly Table number # Enum value: 6
---@field public readonly Tuple number # Enum value: 7
---@field public readonly UserData number # Enum value: 8
---@field public readonly Thread number # Enum value: 9
---@field public readonly ClrFunction number # Enum value: 10
---@field public readonly TailCallRequest number # Enum value: 11
---@field public readonly YieldRequest number # Enum value: 12

--- Read-only enum table
data_type = {}


---
--- Enum: MoonSharp.Interpreter.TypeValidationFlags
--- This enum is read-only and case-insensitive
---
---@class type_validation_flags
---@field public readonly None number # Enum value: 0
---@field public readonly AllowNil number # Enum value: 1
---@field public readonly AutoConvert number # Enum value: 2
---@field public readonly Default number # Enum value: 2

--- Read-only enum table
type_validation_flags = {}



---
--- Class Lilly.Voxel.Plugin.Blocks.BlockType
---
---@class BlockType
---@field id number # Property
---@field name string # Property
---@field is_solid boolean # Property
---@field is_liquid boolean # Property
---@field is_opaque boolean # Property
---@field is_transparent boolean # Property
---@field hardness number # Property
---@field is_breakable boolean # Property
---@field is_billboard boolean # Property
---@field is_item boolean # Property
---@field is_light_source boolean # Property
---@field emits_light number # Property
---@field emits_color Color4b # Property
---@field texture_set BlockTextureSet # Property
---@field render_type block_render_type # Property
---
--- Methods:
---@overload fun(face: block_face, asset_name: string, index: number):nil
---@overload fun(asset_name: string, index: number):nil
---@overload fun(asset_name: string, index: number):nil
---@overload fun(asset_name: string, index: number):nil
---@overload fun(asset_name: string, index: number):nil
---@overload fun(asset_name: string, index: number):nil
---@overload fun(asset_name: string, index: number):nil


---
--- Class MoonSharp.Interpreter.DynValue
---
---@class DynValue
---@field reference_id number # Property
---@field type data_type # Property
---@field function function # Property
---@field number number # Property
---@field tuple DynValue[] # Property
---@field coroutine Coroutine # Property
---@field table Table # Property
---@field boolean boolean # Property
---@field string string # Property
---@field callback CallbackFunction # Property
---@field tail_call_data TailCallData # Property
---@field yield_request YieldRequest # Property
---@field user_data UserData # Property
---@field read_only boolean # Property
---
--- Methods:
---@overload fun():DynValue
---@overload fun():DynValue
---@overload fun(read_only: boolean):DynValue
---@overload fun():DynValue
---@overload fun():string
---@overload fun():string
---@overload fun():string
---@overload fun():number?
---@overload fun():boolean
---@overload fun():any
---@overload fun():DynValue
---@overload fun(value: DynValue):nil
---@overload fun():DynValue
---@overload fun():boolean
---@overload fun():boolean
---@overload fun():boolean
---@overload fun():boolean
---@overload fun():boolean
---@overload fun():any
---@overload fun(desired_type: any):any
---@overload fun():T
---@overload fun(func_name: string, desired_type: data_type, arg_num: number, flags: type_validation_flags):DynValue
---@overload fun(func_name: string, arg_num: number, flags: type_validation_flags):T


---
--- Class MoonSharp.Interpreter.Table
---
---@class Table
---@field owner_script Script # Property
---@field length number # Property
---@field meta_table Table # Property
---@field pairs TablePair[] # Property
---@field keys DynValue[] # Property
---@field values DynValue[] # Property
---@field reference_id number # Property
---
--- Methods:
---@overload fun():nil
---@overload fun(value: DynValue):nil
---@overload fun(key: string, value: DynValue):nil
---@overload fun(key: number, value: DynValue):nil
---@overload fun(key: DynValue, value: DynValue):nil
---@overload fun(key: any, value: DynValue):nil
---@overload fun(keys: any[], value: DynValue):nil
---@overload fun(key: string):DynValue
---@overload fun(key: number):DynValue
---@overload fun(key: DynValue):DynValue
---@overload fun(key: any):DynValue
---@overload fun(keys: any[]):DynValue
---@overload fun(key: string):DynValue
---@overload fun(key: number):DynValue
---@overload fun(key: DynValue):DynValue
---@overload fun(key: any):DynValue
---@overload fun(keys: any[]):DynValue
---@overload fun(key: string):boolean
---@overload fun(key: number):boolean
---@overload fun(key: DynValue):boolean
---@overload fun(key: any):boolean
---@overload fun(keys: any[]):boolean
---@overload fun():nil
---@overload fun(v: DynValue):TablePair?


---
--- Class Lilly.Engine.Core.Data.Privimitives.GameTime
---
---@class GameTime
---@field total_game_time number # Property
---@field elapsed_game_time number # Property
---@field elapsed_game_time_as_time_span number # Property
---@field total_game_time_as_time_span number # Property
---
--- Methods:
---@overload fun():number
---@overload fun():number
---@overload fun(elapsed_seconds: number):nil


--- Global type constructors
GameTime = {}
Table = {}
DynValue = {}
BlockType = {}
Color4b = {}
BlockTextureSet = {}
Coroutine = {}
CallbackFunction = {}
TailCallData = {}
YieldRequest = {}
UserData = {}
T = {}
T = {}
Script = {}
TablePair = {}
