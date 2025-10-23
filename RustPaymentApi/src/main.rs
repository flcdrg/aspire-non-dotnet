use std::env;

use actix_web::{App, HttpResponse, HttpServer, Responder, get, post, web};
use opentelemetry::propagation::Extractor;
use opentelemetry::{
    KeyValue, global,
    trace::{Span, Tracer},
};
use opentelemetry_otlp::WithExportConfig;
use opentelemetry_sdk::{
    Resource, propagation::TraceContextPropagator, runtime::TokioCurrentThread, trace::Config,
};
use rand::{Rng, thread_rng};

// Minimal extractor for Actix HeaderMap to support W3C context extraction
struct ActixHeaderExtractor<'a>(&'a actix_web::http::header::HeaderMap);

impl<'a> Extractor for ActixHeaderExtractor<'a> {
    fn get(&self, key: &str) -> Option<&str> {
        self.0.get(key).and_then(|v| v.to_str().ok())
    }

    fn keys(&self) -> Vec<&str> {
        self.0.keys().map(|k| k.as_str()).collect()
    }
}

#[get("/hello/{name}")]
async fn greet(req: actix_web::HttpRequest, name: web::Path<String>) -> impl Responder {
    let parent_cx =
        global::get_text_map_propagator(|prop| prop.extract(&ActixHeaderExtractor(req.headers())));
    let tracer = global::tracer("rustpaymentapi");
    let mut span = tracer.start_with_context("hello", &parent_cx);
    let msg = format!("Hello {}!", name);
    span.end();
    msg
}

#[derive(serde::Deserialize)]
struct PaymentRequest {
    total_amount: f64,
}

#[derive(serde::Serialize)]
struct PaymentResponse<'a> {
    status: &'a str,
}

#[post("/payment")]
async fn process_payment(
    req: actix_web::HttpRequest,
    payload: web::Json<PaymentRequest>,
) -> HttpResponse {
    let parent_cx =
        global::get_text_map_propagator(|prop| prop.extract(&ActixHeaderExtractor(req.headers())));
    let tracer = global::tracer("rustpaymentapi");
    let mut span = tracer.start_with_context("process_payment", &parent_cx);

    let mut rng = thread_rng();
    let approved = rng.gen_bool(0.8);

    println!(
        "Processed payment of amount {:.2}: {}",
        payload.total_amount,
        if approved { "approved" } else { "declined" }
    );

    let resp = if approved {
        HttpResponse::Ok().json(PaymentResponse { status: "success" })
    } else {
        HttpResponse::Ok().json(PaymentResponse { status: "declined" })
    };

    span.end();
    resp
}

#[actix_web::main] // or #[tokio::main]
async fn main() -> std::io::Result<()> {
    const DEFAULT_HOST: &str = "127.0.0.1";
    const DEFAULT_PORT: &str = "8080";

    let host = env::var("PAYMENT_API_HOST").unwrap_or_else(|_| DEFAULT_HOST.to_string());
    let port_raw = env::var("PAYMENT_API_PORT").unwrap_or_else(|_| DEFAULT_PORT.to_string());

    let port: u16 = port_raw.parse().unwrap_or_else(|_| {
        eprintln!(
            "Invalid PAYMENT_API_PORT value '{port_raw}'. Falling back to default port {DEFAULT_PORT}."
        );
        DEFAULT_PORT
            .parse()
            .expect("Default port should always parse successfully")
    });

    init_telemetry();

    // Create a test span to verify telemetry is working
    let tracer = global::tracer("rustpaymentapi");
    tracer.in_span("startup", |_cx| {
        println!("Starting RustPaymentApi on {host}:{port}");
    });

    HttpServer::new(|| App::new().service(greet).service(process_payment))
        .bind((host.as_str(), port))?
        .run()
        .await?;

    // Ensure all spans have been shipped to Dashboard.
    opentelemetry::global::shutdown_tracer_provider();

    Ok(())
}

fn init_telemetry() {
    global::set_text_map_propagator(TraceContextPropagator::new());

    let otel_endpoint = std::env::var("OTEL_EXPORTER_OTLP_ENDPOINT");

    match otel_endpoint {
        Ok(ref endpoint) => {
            println!("✓ OTEL_EXPORTER_OTLP_ENDPOINT is set: {}", endpoint);

            let resource = Resource::new(vec![KeyValue::new("service.name", "rustpaymentapi")]);

            let tracer = opentelemetry_otlp::new_pipeline()
                .tracing()
                .with_exporter(
                    opentelemetry_otlp::new_exporter()
                        .tonic()
                        .with_endpoint(endpoint),
                )
                .with_trace_config(Config::default().with_resource(resource))
                .install_batch(TokioCurrentThread)
                .expect("Failed to install OpenTelemetry tracer.");

            global::set_tracer_provider(tracer);
            println!("✓ OpenTelemetry tracer configured successfully");
        }
        Err(_) => {
            eprintln!("⚠ OTEL_EXPORTER_OTLP_ENDPOINT not set - traces will not be exported");
            eprintln!("  This is expected when running outside of .NET Aspire");
        }
    }
}
