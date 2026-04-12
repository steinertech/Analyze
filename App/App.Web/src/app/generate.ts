import { NotificationDto } from "./notification.service"

export class RequestDto {
  public commandName!: string
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  public paramList?: any[]
  public developmentSessionId?: string
  public developmentCacheId?: string
  public versionClient?: string
}

export class ResponseDto {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  public result?: any
  public exceptionText?: string
  public navigateUrl?: string
  public notificationList?: NotificationDto[]
  public developmentSessionId?: string
  public developmentCacheId?: string
  public isReload?: boolean
  public cacheCount?: number
}

export class ComponentDto {
  public list?: ComponentDto[]
}

export enum NatificationEnum {
  None = 0,
  Info = 1,
  Success = 2,
  Warning = 3,
  Error = 4,
}

export class ComponentButtonDto extends ComponentDto {
  public text?: string
  public isClick?: boolean
}

export class ComponentTextDto extends ComponentDto {
  public text?: string
}

export class UserDto {
  public email?: string
  public password?: string
}

export class UserStatusDto {
  public email?: string
  public organisationText?: string
}

export class ProductDto {
  public text?: string
  public storageFileName?: string
  public price?: number
}

export class GridRequestDto {
  public grid!: GridDto
  public parentGrid?: GridDto
  public cell?: GridCellDto
  public control?: GridControlDto
  public parentCell?: GridCellDto
  public parentControl?: GridControlDto
}

export class GridResponseDto {
  public grid!: GridDto
  public parentGrid?: GridDto
}

export class GridRequest2EntryDto {
  public grid?: GridDto
  public cell?: GridCellDto
  public control?: GridControlDto
}

export class GridRequest2Dto {
  public list!: GridRequest2EntryDto[]
}

export class GridResponse2Dto {
  public list!: GridDto[]
}

export class GridConfigDto {
  public gridName!: string
  public dataTableName!: string
  public isAllowUpdate?: boolean
  public isAllowInsert?: boolean
  public isAllowDelete?: boolean
  public grid?: GridDto
}

export class GridConfigFieldDto {
  public fieldName!: string
  public text?: string
  public isDropdown?: boolean
}

export class GridCellDto {
  public cellEnum?: GridCellEnum
  public dataRowIndex?: number
  public fieldName?: string
  public text?: string
  public textPlaceholder?: string
  public textModified?: string
  public dropdownList?: (string | null)[]
  public controlList?: GridControlDto[]
  public iconLeft?: GridCellIconDto
  public iconRight?: GridCellIconDto
  public colSpan?: number
  public rowSpan?: number
  public sortIsDesc?: boolean
  public sortIndex?: number
}

export class GridCellIconDto {
  public className?: string
  public tooltip?: string
}

export class GridControlDto {
  public controlEnum?: GridControlEnum
  public text?: string
  public textModified?: string
  public name?: string
  public icon?: GridCellIconDto
  public isDisabled?: boolean
  public isPatch?: boolean
  public fileEnum?: GridFileEnum
  public fileList?: GridFileDto[]
}

export enum GridCellEnum {
  None = 0,
  Field = 1,
  Header = 2,
  HeaderEmpty = 3,
  FieldDropdown = 5,
  Filter = 10,
  FilterEmpty = 14,
  FieldCheckbox = 11,
  FieldAutocomplete = 12,
  CheckboxSelectMulti = 13,
  Control = 16,
}

export enum GridControlEnum {
  None = 0,
  ButtonReload = 1,
  ButtonSave = 2,
  ButtonLookupCancel = 3,
  ButtonLookupOk = 4,
  ButtonLookupSort = 5,
  ButtonColumn = 6,
  Button = 7,
  ButtonCustom = 8,
  CheckboxSelectMultiAll = 9,
  Label = 10,
  FieldCustom = 11,
  ButtonModal = 12,
  ButtonModalCustom = 13,
  Pagination = 14,
  Breadcrumb = 15,
  Title = 16
}

export enum GridFileEnum {
  None = 0,
  Download = 1,
  Upload = 2,
}

export class GridFileDto {
  public fileName?: string
  public fileUrl?: string
}

export class GridDto {
  public gridName!: string
  // dataRowList?: any[]
  // gridConfig?: GridConfigDto
  public rowCellList?: GridCellDto[][]
  public parentGridName?: string
  public state?: GridStateDto
  public patchList?: GridPatchDto[]
  // public editRowIndex?: number
  // public editFieldName?: string
  // public selectRowIndex?: number
  // public selectFieldName?: string
}

export class GridPatchDto {
  public controlName?: string
  public isDisabled?: boolean
  public fileList?: GridFileDto[]
}

export class GridStateDto {
  public sortList?: GridStateSortDto[]
  public filterList?: Record<string, string>
  public filterMultiList?: Record<string, GridStateFilterMultiDto>
  public isMouseEnterList?: (boolean | null)[]
  public isSelectList?: (boolean | null)[]
  public isSelectMultiList?: (boolean | null)[]
  public isSelectMultiAll?: (boolean | null)
  public isSelectMultiIndeterminate?: (boolean | null)
  public columnList?: GridStateColumnDto[]
  public columnWidthList?: (number | null)[]
  public rowKeyList?: (string | null)[]
  public rowKeyMasterList?: Record<string, string | null>
  public pagination?: GridPaginationDto
  public fieldSaveList?: GridCellDto[]
  public fieldCustomSaveList?: FieldCustomSaveDto[]
  public isPatch?: boolean
  public pathList?: GridStatePathDto[]
  public pathModalIndex?: number
}

export class GridStatePathDto {
  public name?: string
  public isModal?: boolean
  public isModalCustom?: boolean
  public icon?: GridCellIconDto
}

export class FieldCustomSaveDto {
  public cell?: GridCellDto
  public control?: GridControlDto
}

export class GridPaginationDto {
  public pageIndex?: number
  public pageCount?: number
  public pageSize?: number
  public pageIndexDeltaClick?: number
}

export class GridStateColumnDto {
  public fieldName!: string
  public orderBy!: number
}

export class GridStateSortDto {
  public fieldName!: string
  public isDesc!: boolean
}

export class GridStateFilterDto {
  public fieldName!: string
  public text!: string
}

export class GridStateFilterMultiDto {
  public textList!: string[]
}
