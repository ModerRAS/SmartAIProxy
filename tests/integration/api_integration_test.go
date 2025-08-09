// +build integration

package integration

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/gin-gonic/gin"
	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/suite"
	
	"SmartAIProxy/internal/gateway"
)

type APIIntegrationTestSuite struct {
	suite.Suite
	assertions *HTTPAssertions
}

type HTTPAssertions struct {
	t *testing.T
}

func NewHTTPAssertions(t *testing.T) *HTTPAssertions {
	return &HTTPAssertions{t: t}
}

func (a *HTTPAssertions) ShouldHaveStatus(resp *http.Response, expected int) {
	assert.Equal(a.t, expected, resp.StatusCode, 
		"Expected status %d, got %d", expected, resp.StatusCode)
}

func (a *HTTPAssertions) ShouldContainHeader(resp *http.Response, key string) {
	assert.Contains(a.t, resp.Header, key, 
		"Expected response to contain header %s", key)
}

func (a *HTTPAssertions) ShouldHaveContentType(resp *http.Response, expected string) {
	actual := resp.Header.Get("Content-Type")
	assert.Contains(a.t, actual, expected, 
		"Expected Content-Type to contain %s, got %s", expected, actual)
}

func (a *HTTPAssertions) ShouldBeSuccess(resp *http.Response) {
	assert.Less(a.t, resp.StatusCode, 400, 
		"Expected success status code, got %d", resp.StatusCode)
}

func (suite *APIIntegrationTestSuite) SetupSuite() {
	gin.SetMode(gin.TestMode)
	suite.assertions = NewHTTPAssertions(suite.T())
}

func (suite *APIIntegrationTestSuite) TestHealthEndpoint() {
	r := gin.New()
	r.GET("/healthz", func(c *gin.Context) {
		c.JSON(200, gin.H{"status": "ok"})
	})

	w := httptest.NewRecorder()
	req, _ := http.NewRequest("GET", "/healthz", nil)
	r.ServeHTTP(w, req)

	suite.assertions.ShouldHaveStatus(w.Result(), http.StatusOK)
	suite.assertions.ShouldHaveContentType(w.Result(), "application/json")
}

func (suite *APIIntegrationTestSuite) TestAPIMiddlewareChain() {
	r := gin.New()
	
	// Add middleware chain
	r.Use(gateway.ErrorHandler())
	r.Use(gateway.RateLimitMiddleware())
	r.Use(gateway.PreprocessMiddleware())
	
	r.GET("/test", func(c *gin.Context) {
		c.JSON(200, gin.H{"message": "success"})
	})

	w := httptest.NewRecorder()
	req, _ := http.NewRequest("GET", "/test", nil)
	r.ServeHTTP(w, req)

	suite.assertions.ShouldBeSuccess(w.Result())
	suite.assertions.ShouldHaveContentType(w.Result(), "application/json")
}

func (suite *APIIntegrationTestSuite) TestPostRequestValidation() {
	r := gin.New()
	r.Use(gateway.PreprocessMiddleware())
	
	r.POST("/test", func(c *gin.Context) {
		c.JSON(200, gin.H{"message": "processed"})
	})

	// Test with valid JSON
	validPayload := map[string]string{"test": "value"}
	jsonPayload, _ := json.Marshal(validPayload)
	
	w := httptest.NewRecorder()
	req, _ := http.NewRequest("POST", "/test", bytes.NewBuffer(jsonPayload))
	req.Header.Set("Content-Type", "application/json")
	r.ServeHTTP(w, req)

	suite.assertions.ShouldBeSuccess(w.Result())
}

func (suite *APIIntegrationTestSuite) TestInvalidContentType() {
	r := gin.New()
	r.Use(gateway.PreprocessMiddleware())
	
	r.POST("/test", func(c *gin.Context) {
		c.JSON(200, gin.H{"message": "processed"})
	})

	w := httptest.NewRecorder()
	req, _ := http.NewRequest("POST", "/test", bytes.NewBufferString("invalid json"))
	req.Header.Set("Content-Type", "text/plain")
	r.ServeHTTP(w, req)

	suite.assertions.ShouldHaveStatus(w.Result(), http.StatusBadRequest)
}

func (suite *APIIntegrationTestSuite) TestAuthMiddlewareWithoutToken() {
	r := gin.New()
	r.Use(gateway.AuthMiddleware())
	
	r.GET("/test", func(c *gin.Context) {
		c.JSON(200, gin.H{"message": "authorized"})
	})

	w := httptest.NewRecorder()
	req, _ := http.NewRequest("GET", "/test", nil)
	r.ServeHTTP(w, req)

	suite.assertions.ShouldHaveStatus(w.Result(), http.StatusUnauthorized)
}

func TestAPIIntegrationSuite(t *testing.T) {
	suite.Run(t, new(APIIntegrationTestSuite))
}