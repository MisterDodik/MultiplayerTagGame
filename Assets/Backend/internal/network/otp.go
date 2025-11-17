package network

import (
	"errors"

	"github.com/google/uuid"
)

type OTP struct {
	username string
	lobby    string
	key      string
}

type RetentionMap map[string]OTP

func NewRetentionMap() RetentionMap {
	return make(RetentionMap)
}

func (rm RetentionMap) NewOTP(username, lobby string) (*OTP, error) {
	if username == "" || lobby == "" {
		return nil, errors.New("username or lobby name empty")
	}

	otp := &OTP{
		username: username,
		lobby:    lobby,
		key:      uuid.NewString(),
	}

	return otp, nil
}

func (rm RetentionMap) ValidateOTP(otp string, username, lobby *string) bool {
	if _, ok := rm[otp]; !ok {
		return false
	}
	*username = rm[otp].username
	*lobby = rm[otp].lobby
	delete(rm, otp)
	return true
}
