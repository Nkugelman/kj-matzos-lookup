import { useState } from "react"
import { COPY, EDIT_COPY } from "./kioskCopy"
import { formatPhoneInput, digitsOnly } from "./phoneUtils"
import type { CustomerProfile } from "./types"

type EditableField =
  | "firstName"
  | "lastName"
  | "email"
  | "address"
  | "primaryPhone"
  | "secondaryPhone"

export interface ProfileFormState {
  firstName: string
  lastName: string
  email: string
  address: string
  primaryPhone: string
  secondaryPhone: string
}

interface KioskEditProfileProps {
  profile: CustomerProfile
  form: ProfileFormState
  saving: boolean
  error: string | null
  saved: boolean
  onSaveField: (field: EditableField, value: string) => Promise<void>
  onBack: () => void
}

function ProfileFieldRow({
  label,
  field,
  value,
  placeholder,
  editing,
  saving,
  onStartEdit,
  onCancel,
  onChange,
  onSave,
}: {
  label: string
  field: EditableField
  value: string
  placeholder: string
  editing: boolean
  saving: boolean
  onStartEdit: () => void
  onCancel: () => void
  onChange: (value: string) => void
  onSave: (field: EditableField, value: string) => void
}) {
  const displayValue =
    field === "primaryPhone" || field === "secondaryPhone"
      ? formatPhoneInput(digitsOnly(value))
      : field === "email"
        ? value.toUpperCase()
        : value

  return (
    <div className="kiosk-edit-row">
      <div className="kiosk-edit-row__actions">
        {editing ? (
          <>
            <button
              type="button"
              className="kiosk-edit-row__btn kiosk-edit-row__btn--cancel"
              onClick={onCancel}
              disabled={saving}
            >
              {EDIT_COPY.cancel}
            </button>
            <button
              type="button"
              className="kiosk-edit-row__btn kiosk-edit-row__btn--save"
              onClick={() => void onSave(field, value)}
              disabled={saving}
            >
              {EDIT_COPY.save}
            </button>
          </>
        ) : (
          <button
            type="button"
            className="kiosk-edit-row__btn"
            onClick={onStartEdit}
            disabled={saving}
          >
            {label}
          </button>
        )}
      </div>
      <div className="kiosk-edit-row__body">
        <p className={`kiosk-edit-row__value ${field === "email" ? "kiosk-edit-row__value--email" : ""}`}>
          {displayValue || "\u00A0"}
        </p>
        {editing && (
          <input
            className="kiosk-edit-row__input"
            type={field === "email" ? "email" : "text"}
            value={value}
            placeholder={placeholder}
            disabled={saving}
            autoFocus
            onChange={(e) => onChange(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") void onSave(field, value)
            }}
          />
        )}
      </div>
    </div>
  )
}

export function profileToForm(profile: CustomerProfile): ProfileFormState {
  return {
    firstName: profile.firstName ?? "",
    lastName: profile.lastName ?? "",
    email: profile.email ?? "",
    address: profile.address ?? "",
    primaryPhone: profile.primaryPhone ?? profile.phones[0] ?? "",
    secondaryPhone: profile.secondaryPhone ?? profile.phones[1] ?? "",
  }
}

export function KioskEditProfile({
  profile,
  form,
  saving,
  error,
  saved,
  onSaveField,
  onBack,
}: KioskEditProfileProps) {
  const [editing, setEditing] = useState<EditableField | null>(null)
  const [draft, setDraft] = useState<ProfileFormState>(form)

  const startEdit = (field: EditableField) => {
    setDraft(form)
    setEditing(field)
  }

  const cancelEdit = () => {
    setDraft(form)
    setEditing(null)
  }

  const updateDraft = (field: EditableField, value: string) => {
    setDraft((prev) => ({ ...prev, [field]: value }))
  }

  const saveField = async (field: EditableField, value: string) => {
    if (value === form[field]) {
      setEditing(null)
      return
    }
    await onSaveField(field, value)
    setEditing(null)
  }

  const fields: Array<{
    field: EditableField
    label: string
    placeholder: string
  }> = [
    { field: "firstName", label: EDIT_COPY.editFirstName, placeholder: EDIT_COPY.editFirstName },
    { field: "lastName", label: EDIT_COPY.editLastName, placeholder: EDIT_COPY.editLastName },
    { field: "email", label: EDIT_COPY.editEmail, placeholder: EDIT_COPY.placeholderEmail },
    { field: "address", label: EDIT_COPY.editAddress, placeholder: EDIT_COPY.placeholderAddress },
    { field: "primaryPhone", label: EDIT_COPY.editPhone, placeholder: EDIT_COPY.placeholderPhone },
    { field: "secondaryPhone", label: EDIT_COPY.editPhone2, placeholder: EDIT_COPY.placeholderPhone2 },
  ]

  return (
    <section className="kiosk-edit">
      <p className="kiosk-banner">{COPY.infoForPhone}</p>
      <p className="kiosk-edit__name">{profile.displayName}</p>
      {error && <div className="kiosk-error">{error}</div>}
      {saved && <p className="kiosk-edit__saved">{EDIT_COPY.profileSaved}</p>}
      <div className="kiosk-edit__fields">
        {fields.map(({ field, label, placeholder }) => (
          <ProfileFieldRow
            key={field}
            field={field}
            label={label}
            placeholder={placeholder}
            value={editing === field ? draft[field] : form[field]}
            editing={editing === field}
            saving={saving}
            onStartEdit={() => startEdit(field)}
            onCancel={cancelEdit}
            onChange={(v) => updateDraft(field, v)}
            onSave={saveField}
          />
        ))}
      </div>
      <button type="button" className="kiosk-edit__back" onClick={onBack} disabled={saving}>
        {EDIT_COPY.back}
      </button>
    </section>
  )
}
