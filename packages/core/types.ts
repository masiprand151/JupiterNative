export type Child = VNode | string | number | null | undefined | boolean | Child[];
export type ComponentType<P = Record<string, unknown>> = (props: P) => Child;
export type VNodeType = string | ComponentType<Record<string, unknown>> | symbol;
export interface VNode { type: VNodeType; props: Record<string, unknown>; key?: string | number; children: Child[]; }
export type StateSetter<T> = (value: T | ((previous: T) => T)) => void;
export type PressHandler = () => void;
export type ColorValue = string;
export type FlexDirection = 'row' | 'column' | 'horizontal' | 'vertical';
export type Alignment = 'auto' | 'flex-start' | 'flex-end' | 'center' | 'stretch' | 'left' | 'right';
export type JustifyContent = 'flex-start' | 'flex-end' | 'center' | 'space-between' | 'space-around' | 'space-evenly';
export interface StyleProps { width?: number; height?: number; minWidth?: number; minHeight?: number; maxWidth?: number; maxHeight?: number; margin?: number | string; padding?: number | string; backgroundColor?: ColorValue; borderColor?: ColorValue; borderWidth?: number; borderRadius?: number; opacity?: number; flex?: number; flexGrow?: number; flexShrink?: number; gap?: number; direction?: FlexDirection; alignItems?: Alignment; alignment?: Alignment; justifyContent?: JustifyContent; fontSize?: number; fontWeight?: string; color?: ColorValue; textAlign?: string; }
export interface CommonProps extends StyleProps { children?: Child; key?: string | number; id?: string; testID?: string; accessible?: boolean; accessibilityLabel?: string; style?: StyleProps; }
export interface WindowProps extends CommonProps { title?: string; onClose?: () => void; }
export interface WindowStartPosition { type: 'center' | 'manual'; x?: number; y?: number; }
export interface WindowOptions {
  title?: string;
  icon?: string;
  width?: number;
  height?: number;
  minWidth?: number;
  minHeight?: number;
  maxWidth?: number;
  maxHeight?: number;
  resizable?: boolean;
  titleBar?: boolean;
  alwaysOnTop?: boolean;
  fullscreen?: boolean;
  startPosition?: WindowStartPosition;
}
export interface ViewProps extends CommonProps {}
export interface SafeAreaViewProps extends ViewProps {}
export interface TextProps extends CommonProps { value?: string | number; fontSize?: number; fontWeight?: string; color?: ColorValue; textAlign?: string; lineHeight?: number; numberOfLines?: number; selectable?: boolean; }
export interface ButtonProps extends CommonProps { title: string; onPress?: PressHandler; disabled?: boolean; color?: ColorValue; }
export interface PressableProps extends CommonProps { onPress?: PressHandler; disabled?: boolean; }
export interface ActivityIndicatorProps extends CommonProps { animating?: boolean; color?: ColorValue; size?: 'small' | 'large' | number; }
export interface ModalProps extends CommonProps { visible?: boolean; transparent?: boolean; onRequestClose?: () => void; }
export interface SliderProps extends CommonProps { value?: number; minimumValue?: number; maximumValue?: number; step?: number; disabled?: boolean; onValueChange?: (value: number) => void; }
export interface ProgressBarProps extends CommonProps { value?: number; minimum?: number; maximum?: number; color?: ColorValue; }
export interface CheckBoxProps extends CommonProps { value?: boolean; defaultValue?: boolean; disabled?: boolean; label?: string; onValueChange?: (value: boolean) => void; }
export interface RadioButtonProps extends CommonProps { value?: boolean; disabled?: boolean; label?: string; onValueChange?: (value: boolean) => void; }
export interface DatePickerProps extends CommonProps { value?: string; disabled?: boolean; onChange?: (value: string) => void; }
export interface SwitchProps extends CommonProps { value?: boolean; defaultValue?: boolean; disabled?: boolean; onValueChange?: (value: boolean) => void; }
export interface ImageProps extends CommonProps { source: string | { uri: string }; resizeMode?: 'cover' | 'contain' | 'stretch'; alt?: string; }
export interface TextInputProps extends CommonProps { value?: string; defaultValue?: string; placeholder?: string; onChangeText?: (value: string) => void; onSubmitEditing?: (value: string) => void; onFocus?: () => void; onBlur?: () => void; disabled?: boolean; editable?: boolean; secureTextEntry?: boolean; multiline?: boolean; maxLength?: number; autoFocus?: boolean; }
export interface ScrollViewProps extends CommonProps { horizontal?: boolean; showsVerticalScrollIndicator?: boolean; showsHorizontalScrollIndicator?: boolean; keyboardShouldPersistTaps?: boolean; }
export interface FlatListProps<T = unknown> extends CommonProps { data: T[]; renderItem: (info: { item: T; index: number }) => Child; keyExtractor?: (item: T, index: number) => string; horizontal?: boolean; }
export type NavigationParams = Record<string, unknown>;
export interface NavigationRoute { name: string; params?: NavigationParams; }
export interface NavigationApi { navigate: (route: string, params?: NavigationParams) => void; goBack: () => void; replace: (route: string, params?: NavigationParams) => void; }
export interface NavigationScreenProps { route: NavigationRoute; navigation: NavigationApi; }
export interface NavigationProps { initialRouteName: string; screens: Record<string, ComponentType<NavigationScreenProps>>; }
export const Fragment = Symbol.for('jupiter.fragment');
declare global { namespace JSX { interface IntrinsicElements { Window: WindowProps; View: ViewProps; SafeAreaView: SafeAreaViewProps; Text: TextProps; Button: ButtonProps; Pressable: PressableProps; TouchableOpacity: PressableProps; ActivityIndicator: ActivityIndicatorProps; Switch: SwitchProps; Modal: ModalProps; Slider: SliderProps; ProgressBar: ProgressBarProps; CheckBox: CheckBoxProps; RadioButton: RadioButtonProps; DatePicker: DatePickerProps; Image: ImageProps; TextInput: TextInputProps; ScrollView: ScrollViewProps; FlatList: FlatListProps; } } }
